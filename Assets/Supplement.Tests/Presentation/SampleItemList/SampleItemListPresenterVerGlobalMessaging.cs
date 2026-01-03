using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Tests.Presentation.Abstractions;
using Supplement.Core;
using Supplement.Tests.Application.Abstractions;

namespace Supplement.Tests.Presentation
{
    public class SampleItemListPresenterVerGlobalMessaging : ISampleItemListPresenter
    {
        private readonly ViewDto viewDto;
        private readonly ISampleItemListViewDtoFactory dtoFactory;
        private readonly IItemService itemService;
        private readonly CancellationTokenSource cts = new();

        private bool waitingForPop;
        private bool useGlobalMessaging;
        private IView view;
        
        public SampleItemListPresenterVerGlobalMessaging(ViewDto viewDto, ISampleItemListViewDtoFactory dtoFactory,
            IMessageBroker messageBroker, IItemService itemService)
        {
            this.viewDto = viewDto;
            this.dtoFactory = dtoFactory;
            this.itemService = itemService;
            
            messageBroker.Subscribe<IncrementItemAmountMessage>(x => AddItemAmount(x.ItemId));
            messageBroker.Subscribe<DecrementItemAmountMessage>(x => SubtractItemAmount(x.ItemId));
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