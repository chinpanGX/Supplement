using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Supplement.Core.Abstractions;
using Supplement.Loader.Abstractions;
using Supplement.Tests.Application;
using Supplement.Tests.Application.Abstractions;
using Supplement.Tests.Domain;
using Supplement.Tests.Infrastructure;
using VContainer;
using Supplement.Tests.Presentation;
using Supplement.Tests.Presentation.Abstractions;
using Supplement.ZeroMessenger;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer.Unity;

namespace Supplement.Tests.PlayMode
{
    public class TestShowSamplePopup
    {
        private CancellationTokenSource cts;

        [UnityTest]
        public IEnumerator TestShowSample()
        {
            cts = new CancellationTokenSource();
            var builder = new ContainerBuilder();
            builder.RegisterAddressablesLoader();
            builder.Register<ISamplePopupDtoFactory, SamplePopupDtoFactory>(Lifetime.Scoped);
            builder.Register<IPresenter, SamplePopupPresenter>(Lifetime.Transient);
            var rootContainer = builder.Build();
            var assetLoader = rootContainer.Resolve<IAssetLoader>();
            yield return TestHelper.SetupEnvAsync(assetLoader, cts.Token).ToCoroutine();

            var dtoFactory = rootContainer.Resolve<ISamplePopupDtoFactory>();
            var dto = dtoFactory.CreateSamplePopupDto();
            var popupScope = rootContainer.CreateScope(x => { x.RegisterInstance<ViewDto>(dto); });
            yield return TestRenderSamplePopupAsync(popupScope, assetLoader).ToCoroutine();

            popupScope.Dispose();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            cts.Cancel();
            cts.Dispose();
        }

        private async UniTask TestRenderSamplePopupAsync(IScopedObjectResolver scopedObjectResolver,
            IAssetLoader assetLoader)
        {
            var presenter = scopedObjectResolver.Resolve<IPresenter>();
            var view = await LoadSamplePopupViewAsync(scopedObjectResolver, assetLoader);
            await presenter.RenderAsync(view);
            await UniTask.WaitForSeconds(2.0f, cancellationToken: cts.Token);
        }

        private async UniTask<IView> LoadSamplePopupViewAsync(IScopedObjectResolver scopedObjectResolver,
            IAssetLoader assetLoader)
        {
            var handle = await assetLoader.LoadAssetAsync<GameObject>(
                "Assets/Supplement.Tests/Addressables/SamplePopupView.prefab",
                cts.Token
            );

            var obj = scopedObjectResolver.Instantiate(handle.Result);
            var component = obj.GetComponent<SamplePopupView>();
            return component;
        }
    }
}
