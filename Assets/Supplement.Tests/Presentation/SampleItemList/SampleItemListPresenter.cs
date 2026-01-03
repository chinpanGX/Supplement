using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Tests.Application.Abstractions;
using Supplement.Tests.Presentation.Abstractions;

namespace Supplement.Tests.Presentation
{
    public class SampleItemListPresenter : ISampleItemListPresenter
    {
        private readonly ViewDto viewDto;
        private readonly ISampleItemListViewDtoFactory dtoFactory;
        private readonly IItemService itemService;
        private readonly CancellationTokenSource cts = new();

        private bool waitingForPop;
        private bool useGlobalMessaging;
        private IView view;
        
        public SampleItemListPresenter(ViewDto viewDto, ISampleItemListViewDtoFactory dtoFactory,
            IItemService itemService)
        {
            this.viewDto = viewDto;
            this.dtoFactory = dtoFactory;
            this.itemService = itemService;
        }
        
        public void Dispose()
        {
            cts?.Cancel();
            cts?.Dispose();
            view?.Dispose();
        }
        
        public UniTask RenderAsync(IView view)
        {
            if (viewDto is ItemListViewDto dto)
            {
                useGlobalMessaging = dto.UseGlobalMessaging;
                waitingForPop = true;
            }
            this.view = view;
            return view.RenderAsync(viewDto);
        }
        
        public void Pop()
        {
            waitingForPop = false;
        }
        
        public UniTask WaitForPopCompletionAsync()
        {
            return UniTask.WaitUntil(() => !waitingForPop, cancellationToken: cts.Token);
        }
        
        public void AddItemAmount(int itemId)
        {
            async UniTaskVoid AddAsync()
            {
                await itemService.AddAmountAsync(itemId, 1, cts.Token);
                await RefreshAsync();
            }
            AddAsync().Forget();
        }
        
        public void SubtractItemAmount(int itemId)
        {
            async UniTaskVoid SubtractAsync()
            {
                await itemService.SubtractAmountAsync(itemId, 1, cts.Token);
                await RefreshAsync();
            }
            SubtractAsync().Forget();
        }

        private UniTask RefreshAsync()
        {
            var newDto = dtoFactory.CreateDto(useGlobalMessaging);
            view.RenderAsync(newDto);
            return UniTask.CompletedTask;
        }
    }
}