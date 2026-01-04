using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core;
using Supplement.Loader.Abstractions;
using Supplement.Tests.Application;
using Supplement.Tests.Application.Abstractions;
using Supplement.Tests.Domain;
using Supplement.Tests.Infrastructure;
using Supplement.Tests.Presentation;
using Supplement.Tests.Presentation.Abstractions;
using Supplement.VContainer;
using Supplement.ZeroMessenger;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace Supplement.Tests.PlayMode
{
    public class TestItemListView
    {
        private CancellationTokenSource cts = new();

        [UnityTest]
        public IEnumerator TestShowSampleItemListUseGlobalMessaging()
        {
            async UniTask Run()
            {
                cts = new CancellationTokenSource();
                var builder = new ContainerBuilder();
                RegisterCommonServices(builder);
                
                builder.Register<IMessageBroker, GlobalMessageBroker>(Lifetime.Singleton);
                builder.Register<SampleItemListPresenterVerGlobalMessaging>(Lifetime.Scoped)
                    .As<IPresenter, ISampleItemListPresenter>();
                var rootContainer = builder.Build();
                
                // Set up test environment
                rootContainer.Resolve<IFileStorageService>().SetDirectoryName("SupplementTest");
                await rootContainer.Resolve<IItemService>().GrantDummyItemsAsync(cts.Token);
                ObjectResolverGateway.Register(rootContainer);
                var assetLoader = rootContainer.Resolve<IAssetLoader>();
                await TestHelper.SetupEnvAsync(assetLoader, cts.Token);

                var dtoFactory = rootContainer.Resolve<ISampleItemListViewDtoFactory>();
                var dto = dtoFactory.CreateDtoForUseGlobalMessaging();
                
                var scopedObjectResolver = rootContainer.CreateScope(x => { x.RegisterInstance<ViewDto>(dto); });
                var presenter = scopedObjectResolver.Resolve<ISampleItemListPresenter>();
                var view = await LoadSamplePopupViewAsync(scopedObjectResolver, assetLoader);
                await presenter.RenderAsync(view);
                await presenter.WaitForPopCompletionAsync();
                
                scopedObjectResolver.Dispose();
            }
            
            yield return Run().ToCoroutine();
        }

        [UnityTest]
        public IEnumerator TestShowSampleItemListUseHierarchyMessaging()
        {
            async UniTask Run()
            {
                cts = new CancellationTokenSource();
                var builder = new ContainerBuilder();
                RegisterCommonServices(builder);
                
                builder.Register<SampleItemListPresenter>(Lifetime.Scoped)
                    .As<IPresenter, ISampleItemListPresenter>();
                var rootContainer = builder.Build();
                
                // Set up test environment
                rootContainer.Resolve<IFileStorageService>().SetDirectoryName("SupplementTest");
                await rootContainer.Resolve<IItemService>().GrantDummyItemsAsync(cts.Token);
                var assetLoader = rootContainer.Resolve<IAssetLoader>();
                await TestHelper.SetupEnvAsync(assetLoader, cts.Token);

                var dtoFactory = rootContainer.Resolve<ISampleItemListViewDtoFactory>();
                var dto = dtoFactory.CreateDtoForUseHierarchyMessaging();
                
                var scopedObjectResolver = rootContainer.CreateScope(x => { x.RegisterInstance<ViewDto>(dto); });
                var presenter = scopedObjectResolver.Resolve<ISampleItemListPresenter>();
                var view = await LoadSamplePopupViewAsync(scopedObjectResolver, assetLoader);
                await presenter.RenderAsync(view);
                await presenter.WaitForPopCompletionAsync();
                
                scopedObjectResolver.Dispose();
            }
            yield return Run().ToCoroutine();
        }
        
        [TearDown]
        public void AfterTest()
        {
            var path = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "SupplementTest");
            if (System.IO.Directory.Exists(path))
            {
                System.IO.Directory.Delete(path, true);
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            cts.Cancel();
            cts.Dispose();
        }
        
        private void RegisterCommonServices(ContainerBuilder builder)
        {
            builder.RegisterAddressablesLoader();
            builder.RegisterJsonFileStorage();
            builder.Register<IItemRepository, ItemRepository>(Lifetime.Singleton);
            builder.Register<IItemService, ItemService>(Lifetime.Singleton);
            builder.Register<ISampleItemListViewDtoFactory, SampleItemListDtoFactory>(Lifetime.Scoped);
        }

        private async UniTask<IView> LoadSamplePopupViewAsync(IScopedObjectResolver scopedObjectResolver,
            IAssetLoader assetLoader)
        {
            var handle = await assetLoader.LoadAssetAsync<GameObject>(
                "Assets/Supplement.Tests/Addressables/SampleItemListView.prefab",
                cts.Token
            );

            var obj = scopedObjectResolver.Instantiate(handle.Result);
            var component = obj.GetComponent<SampleItemListView>();
            return component;
        }
    }
}