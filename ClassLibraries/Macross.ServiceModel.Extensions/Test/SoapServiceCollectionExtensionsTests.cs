using System;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.ServiceModel.Extensions.Tests
{
	[TestClass]
	public class SoapServiceCollectionExtensionsTests
	{
		private class CustomEndpointBehavior : IEndpointBehavior
		{
			public bool ClientBehaviorApplied { get; private set; }

			public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
			{
			}

			public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
				=> ClientBehaviorApplied = true;

			public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
			{
			}

			public void Validate(ServiceEndpoint endpoint)
			{
			}
		}

		[ServiceContract]
		private interface ITestProxy
		{
			CommunicationState ChannelState { get; }

			[OperationContract]
			int GetStatus();

			[OperationContract]
			void SendStatus(int status);
		}

#pragma warning disable CA1812 // Remove class never instantiated
		private class TestProxy : ITestProxy
#pragma warning restore CA1812 // Remove class never instantiated
		{
			private readonly SoapClient<ITestProxy> _SoapClient;

			public CommunicationState ChannelState => _SoapClient.State;

			public TestProxy(SoapClient<ITestProxy> soapClient)
			{
				_SoapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
			}

			public int GetStatus() => _SoapClient.Channel.GetStatus();

			public void SendStatus(int status) => _SoapClient.Channel.SendStatus(status);
		}

		[TestMethod]
		public void AddSoapClientProxyTest()
		{
			ServiceCollection ServiceCollection = new ServiceCollection();

			ServiceCollection
				.AddSoapClient<ITestProxy, TestProxy>((serviceProvider, soapClientFactory)
					=> new ChannelFactory<ITestProxy>(new BasicHttpBinding(), new EndpointAddress("http://localhost:9999/")));

			ServiceProvider ServiceProvider = ServiceCollection.BuildServiceProvider();

			ITestProxy TestProxy;

			using (IServiceScope Scope = ServiceProvider.CreateScope())
			{
				TestProxy = Scope.ServiceProvider.GetRequiredService<ITestProxy>();

				Assert.IsNotNull(TestProxy);

				try
				{
					TestProxy.GetStatus();
				}
				catch (CommunicationException)
				{
				}
			}

			Assert.AreEqual(CommunicationState.Closed, TestProxy.ChannelState);
		}

		[TestMethod]
		public void EndpointBehaviorTest()
		{
			ServiceCollection ServiceCollection = new ServiceCollection();

			CustomEndpointBehavior? CustomEndpointBehavior = null;

			ServiceCollection
				.AddSoapClient<ITestProxy, TestProxy>((serviceProvider, soapClientFactory)
					=> new ChannelFactory<ITestProxy>(new BasicHttpBinding(), new EndpointAddress("http://localhost:9999/")))
				.AddEndpointBehavior((serviceProvider) =>
				{
					CustomEndpointBehavior = new CustomEndpointBehavior();
					return CustomEndpointBehavior;
				});

			ServiceProvider ServiceProvider = ServiceCollection.BuildServiceProvider();

			ITestProxy TestProxy = ServiceProvider.GetRequiredService<ITestProxy>();

			Assert.IsNotNull(CustomEndpointBehavior);
			Assert.IsTrue(CustomEndpointBehavior.ClientBehaviorApplied);
		}

		[TestMethod]
		public void InvalidateSoapClientChannelFactoryTest()
		{
			ServiceCollection ServiceCollection = new ServiceCollection();

			int ChannelFactoryInstancesCreated = 0;
			ISoapClientFactory? SoapClientFactory = null;

			ServiceCollection
				.AddSoapClient<ITestProxy, TestProxy>((serviceProvider, soapClientFactory) =>
				{
					SoapClientFactory = soapClientFactory;
					ChannelFactoryInstancesCreated++;
					return new ChannelFactory<ITestProxy>(new BasicHttpBinding(), new EndpointAddress("http://localhost:9999/"));
				});

			ServiceProvider ServiceProvider = ServiceCollection.BuildServiceProvider();

			ITestProxy TestProxy = ServiceProvider.GetRequiredService<ITestProxy>();

			Assert.IsNotNull(TestProxy);

			TestProxy = ServiceProvider.GetRequiredService<ITestProxy>();

			Assert.IsNotNull(TestProxy);

			Assert.IsNotNull(SoapClientFactory);

			SoapClientFactory.Invalidate<ITestProxy>();

			TestProxy = ServiceProvider.GetRequiredService<ITestProxy>();

			Assert.IsNotNull(TestProxy);

			Assert.AreEqual(2, ChannelFactoryInstancesCreated);
		}
	}
}
