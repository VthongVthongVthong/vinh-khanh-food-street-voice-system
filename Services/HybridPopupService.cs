using System;
using System.Collections.Generic;
using System.Diagnostics;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    public class QueuedPOI
    {
     public POI Poi { get; set; } = null!;
      public double Distance { get; set; }
        public DateTime TriggeredAtUtc { get; set; }
    }

    public class HybridPopupService
    {
      private static HybridPopupService? _instance;
  private static readonly object _instanceLock = new();

     private System.Collections.ObjectModel.ObservableCollection<POI> _activePOIs;
  private POI? _selectedPOI;
      private bool _isVisible;
   private CancellationTokenSource? _autoDismissCts;
        private readonly TimeSpan _autoDismissDelay = TimeSpan.FromSeconds(40);

        private readonly PriorityQueue<QueuedPOI, (int PriorityDesc, double DistanceAsc, long TriggeredTicks)> _poiQueue;
   private readonly int _maxActivePOIs = 5;
      private readonly int _maxQueueSize = 10;

        private readonly HashSet<int> _activePOIIds = new();
         private readonly HashSet<int> _queuedPOIIds = new();
        private readonly object _syncLock = new();

    public event EventHandler<POI>? PopupRequested;
      public event EventHandler<(POI OldPOI, POI NewPOI)>? POISelectionChanged;
  public event EventHandler<POI>? POIAddedToActive;
       public event EventHandler<POI>? POIRemovedFromActive;
  public event EventHandler? PopupClosed;
         public event EventHandler? QueueUpdated;

  private HybridPopupService()
 {
        _activePOIs = new System.Collections.ObjectModel.ObservableCollection<POI>();
   _poiQueue = new();
 _selectedPOI = null;
      _isVisible = false;

   Debug.WriteLine("[HybridPopupService] ? Initialized");
        }

        public static HybridPopupService Instance
 {
         get
      {
   if (_instance == null)
  {
          lock (_instanceLock)
     {
        _instance ??= new HybridPopupService();
         }
  }
   return _instance;
            }
        }

#region Public Properties

        public System.Collections.ObjectModel.ObservableCollection<POI> ActivePOIs => _activePOIs;

   public POI? SelectedPOI
    {
 get => _selectedPOI;
   private set => _selectedPOI = value;
     }

        public bool IsVisible => _isVisible;

        public int QueueCount
        {
  get
   {
            lock (_syncLock)
  {
   return _poiQueue.Count;
   }
 }
        }

    public int ActiveCount => _activePOIs.Count;

        #endregion

        #region Core Methods

        public async Task HandleIncomingPOIAsync(POI poi, double distance)
        {
  if (poi == null)
   {
    Debug.WriteLine($"[HybridPopupService] ? Null POI");
           return;
   }

try
         {
       lock (_syncLock)
        {
          // ? FIX: Only block if ACTIVELY DISPLAYING
           if (_activePOIIds.Contains(poi.Id))
      {
    Debug.WriteLine($"[HybridPopupService] ?? POI {poi.Id} already displayed");
      return;
           }

      if (_queuedPOIIds.Contains(poi.Id))
         {
     Debug.WriteLine($"[HybridPopupService] ?? POI {poi.Id} already queued");
      return;
       }
        }

     // ? FIX: Minimal logging for performance
Debug.WriteLine($"[HybridPopupService] ?? State: Visible={_isVisible}, Active={_activePOIs.Count}, Queued={_poiQueue.Count}");

     if (!_isVisible)
          {
    Debug.WriteLine($"[HybridPopupService] ?? Showing POI {poi.Id}");
        await ShowNewPopupAsync(poi, distance);
         return;
 }

      Debug.WriteLine($"[HybridPopupService] ?? Adding POI {poi.Id} to system");
       await AddPOIToSystemAsync(poi, distance);
    }
catch (Exception ex)
         {
    Debug.WriteLine($"[HybridPopupService] ? Error: {ex.Message}");
   }
        }

    private async Task ShowNewPopupAsync(POI poi, double distance)
   {
 try
    {
Debug.WriteLine($"[HybridPopupService] ?? ShowNewPopupAsync for POI {poi.Id}");

    _isVisible = true;
_selectedPOI = poi;

        lock (_syncLock)
         {
      _activePOIIds.Add(poi.Id);
     }

          // ? FIX: Use MainThread dispatch (non-blocking, fire-and-forget)
     MainThread.BeginInvokeOnMainThread(() =>
     {
    _activePOIs.Add(poi);
    PopupRequested?.Invoke(this, poi);
 Debug.WriteLine($"[HybridPopupService] ? Event dispatched for POI {poi.Id}");
       });

    CancelAutoDismiss();
                ScheduleAutoDismiss();
      }
  catch (Exception ex)
        {
    Debug.WriteLine($"[HybridPopupService] ? Error: {ex.Message}");
           _isVisible = false;
    }
        }

        private async Task AddPOIToSystemAsync(POI poi, double distance)
  {
         try
 {
            lock (_syncLock)
    {
            if (_activePOIs.Count < _maxActivePOIs)
        {
     _activePOIIds.Add(poi.Id);

     MainThread.BeginInvokeOnMainThread(() =>
         {
    _activePOIs.Add(poi);
SortActivePOIsUI();
       POIAddedToActive?.Invoke(this, poi);
         });

       ScheduleAutoDismiss();
           return;
    }

      if (_poiQueue.Count < _maxQueueSize)
      {
     var queuedPOI = new QueuedPOI
      {
   Poi = poi,
      Distance = distance,
TriggeredAtUtc = DateTime.UtcNow
 };

    var priority = (-poi.Priority, distance, queuedPOI.TriggeredAtUtc.Ticks);
  _poiQueue.Enqueue(queuedPOI, priority);
   _queuedPOIIds.Add(poi.Id);

      Debug.WriteLine($"[HybridPopupService] ?? Queued POI {poi.Id}");
    MainThread.BeginInvokeOnMainThread(() => QueueUpdated?.Invoke(this, EventArgs.Empty));
     return;
 }

     if (_poiQueue.Count >= _maxQueueSize)
        {
    var removedQueued = _poiQueue.Dequeue();
   _queuedPOIIds.Remove(removedQueued.Poi.Id);

      var queuedPOI = new QueuedPOI
     {
 Poi = poi,
   Distance = distance,
   TriggeredAtUtc = DateTime.UtcNow
      };
  var priority = (-poi.Priority, distance, queuedPOI.TriggeredAtUtc.Ticks);
    _poiQueue.Enqueue(queuedPOI, priority);
 _queuedPOIIds.Add(poi.Id);

    Debug.WriteLine($"[HybridPopupService] ?? Replaced in full queue");
     MainThread.BeginInvokeOnMainThread(() => QueueUpdated?.Invoke(this, EventArgs.Empty));
}
  }
   await Task.CompletedTask;
      }
 catch (Exception ex)
    {
   Debug.WriteLine($"[HybridPopupService] ? Error: {ex.Message}");
        }
        }

        public async Task SelectPOIAsync(POI poi)
       {
         if (poi == null || !_activePOIs.Contains(poi))
  return;

        try
    {
        var oldPOI = _selectedPOI;
       _selectedPOI = poi;

   MainThread.BeginInvokeOnMainThread(() =>
  {
    POISelectionChanged?.Invoke(this, (oldPOI!, poi));
      Debug.WriteLine($"[HybridPopupService] ?? Selected {poi.Id}");
  });

       ScheduleAutoDismiss();
  }
           catch (Exception ex)
    {
    Debug.WriteLine($"[HybridPopupService] ? Error: {ex.Message}");
       }
   }

  public async Task RemovePOIFromActiveAsync(POI poi)
      {
 if (poi == null)
  return;

try
      {
lock (_syncLock)
      {
      if (!_activePOIIds.Contains(poi.Id))
   return;

        _activePOIIds.Remove(poi.Id);
           }

     MainThread.BeginInvokeOnMainThread(() =>
       {
    _activePOIs.Remove(poi);
    POIRemovedFromActive?.Invoke(this, poi);
          Debug.WriteLine($"[HybridPopupService] ? Removed {poi.Id}");
      });

        if (_selectedPOI?.Id == poi.Id)
      {
         if (_activePOIs.Count > 0)
     {
   await SelectPOIAsync(_activePOIs[0]);
   }
       else
     {
  _selectedPOI = null;
 }
    }

  await PromoteFromQueueAsync();
       }
          catch (Exception ex)
  {
  Debug.WriteLine($"[HybridPopupService] ? Error: {ex.Message}");
            }
    }

        private async Task PromoteFromQueueAsync()
   {
      try
            {
POI? promotedPoi = null;

             lock (_syncLock)
        {
    if (_poiQueue.Count == 0 || _activePOIs.Count >= _maxActivePOIs)
          return;

 var promoted = _poiQueue.Dequeue();
promotedPoi = promoted.Poi;
 _queuedPOIIds.Remove(promoted.Poi.Id);
           _activePOIIds.Add(promoted.Poi.Id);
       }

 if (promotedPoi != null)
      {
 MainThread.BeginInvokeOnMainThread(() =>
   {
  _activePOIs.Add(promotedPoi);
   SortActivePOIsUI();
QueueUpdated?.Invoke(this, EventArgs.Empty);
             Debug.WriteLine($"[HybridPopupService] ?? Promoted {promotedPoi.Id}");
         });
     }
   }
           catch (Exception ex)
  {
    Debug.WriteLine($"[HybridPopupService] ? Error: {ex.Message}");
    }
    }

   public async Task ClosePopupAsync()
          {
   if (!_isVisible)
       return;

try
         {
 CancelAutoDismiss();
           _isVisible = false;

        lock (_syncLock)
     {
   _activePOIIds.Clear();
     _queuedPOIIds.Clear();
       _poiQueue.Clear();
      }

       MainThread.BeginInvokeOnMainThread(() =>
         {
     _activePOIs.Clear();
         _selectedPOI = null;
    PopupClosed?.Invoke(this, EventArgs.Empty);
    Debug.WriteLine("[HybridPopupService] ?? Popup closed");
   });
    }
  catch (Exception ex)
          {
 Debug.WriteLine($"[HybridPopupService] ? Error: {ex.Message}");
    }
        }

        #endregion

        #region Sorting & Utilities

   private void SortActivePOIsUI()
      {
    var sorted = _activePOIs
   .OrderByDescending(p => p.Priority)
 .ThenBy(p => p.DistanceFromUser)
        .ToList();

   _activePOIs.Clear();
 foreach (var poi in sorted)
    {
  _activePOIs.Add(poi);
    }
    }

        public string GetQueueIndicator()
  {
         lock (_syncLock)
       {
   if (_poiQueue.Count == 0)
      return "";
         return $"+{_poiQueue.Count} nearby";
            }
        }

        #endregion

        #region Auto-dismiss

     private void ScheduleAutoDismiss()
      {
    CancelAutoDismiss();
   _autoDismissCts = new CancellationTokenSource();

  _ = Task.Delay(_autoDismissDelay, _autoDismissCts.Token).ContinueWith(async t =>
  {
    if (!t.IsCanceled && _isVisible)
 {
         Debug.WriteLine("[HybridPopupService] ?? Auto-dismiss");
 await ClosePopupAsync();
       }
           });
   }

       private void CancelAutoDismiss()
 {
           _autoDismissCts?.Cancel();
         _autoDismissCts?.Dispose();
        _autoDismissCts = null;
     }

        #endregion
    }
}
