using NUnit.Framework;
using Cysharp.Threading.Tasks;
using Supplement.Tests.Presentation;
using Supplement.Tests.Presentation.Abstractions;
using VContainer;

namespace Supplement.Tests.PlayMode
{
    public class TestShowSamplePopup
    {
        [Test]
        public void TestPopupScopeRunsPopupLifecycle()
        {
            var builder = new ContainerBuilder();
            builder.Register<ISamplePopupDtoFactory, SamplePopupDtoFactory>(Lifetime.Singleton);
            builder.Register<IPresenter, SamplePopupPresenter>(Lifetime.Scoped);
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
    }
}
