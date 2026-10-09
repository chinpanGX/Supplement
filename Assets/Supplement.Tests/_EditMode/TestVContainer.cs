using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core;
using Supplement.Loader.Abstractions;
using Supplement.Loader.AddressablesLoader;
using Supplement.Tests.Application.Abstractions;
using Supplement.Tests.Application;
using Supplement.Tests.Domain;
using Supplement.Tests.Infrastructure;
using Supplement.Tests.Presentation.Abstractions;
using Supplement.Tests.Presentation;
using Supplement.Unity;
using UnityEngine.TestTools;
using UnityEngine;
using VContainer;

namespace Supplement.Tests.EditMode
{
    public class TestVContainer
    {
        [Test]
        public void TestPopupScopeRunsPopupLifecycle()
        {
            var builder = new ContainerBuilder();
            builder.Register<ISamplePopupDtoFactory, SamplePopupDtoFactory>(Lifetime.Scoped);
            builder.Register<IPresenter, SamplePopupPresenter>(Lifetime.Transient);
            var rootContainer = builder.Build();
            
            // dtoの生成
            var dtoFactory = rootContainer.Resolve<ISamplePopupDtoFactory>();
            var dto = dtoFactory.CreateSamplePopupDto();
            
            // popup用のスコープを生成して、そこでPresenterとViewのライフサイクルを実行する
            var popupScope = rootContainer.CreateScope(scopeBuilder =>
            {
                scopeBuilder.RegisterInstance<ViewDto>(dto);
            });
            
            var presenter = popupScope.Resolve<IPresenter>();
            var popupView = new SamplePopupDebugLogView(); 
            popupScope.Inject(popupView);
            
            presenter.RenderAsync(popupView).Forget();
            popupScope.Dispose();
        }

        [UnityTest]
        public IEnumerator TestCreateItemDtos()
        {
            var builder = new ContainerBuilder();
            builder.RegisterEncryptedFileStorage();
            builder.Register<IItemRepository, ItemRepository>(Lifetime.Scoped);
            builder.Register<IItemService, ItemService>(Lifetime.Scoped);
            var rootContainer = builder.Build();

            var itemService = rootContainer.Resolve<IItemService>();
            yield return itemService.GrantDummyItemsAsync().ToCoroutine();

            var dtos = itemService.GetAll().ToEquatableReadOnlyList();
            
            for (int i = 0; i < dtos.Count; i++)
            {
                Assert.AreEqual(1000 + i + 1, dtos[i].Id);
                Assert.AreEqual(10, dtos[i].Amount);
                Debug.Log("ItemDto Id: " + dtos[i].Id + ", Amount: " + dtos[i].Amount);
            }
        }
        
        [OneTimeTearDown]
        public void Dispose()
        {
            var path = Path.Combine(UnityEngine.Application.persistentDataPath, "SupplementTest");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

    }
}