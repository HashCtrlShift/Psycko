using System;
using System.Collections.Generic;
using System.Threading;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Validation;
using Psycko.Core.Services;
using Psycko.Presentation.Logic;
using Psycko.Presentation.Models;
using UnityEngine;
namespace Psycko.Presentation.Controllers
{
 public sealed class GameplayController : MonoBehaviour
 {
  private const int HumanPlayerId=0;
  [SerializeField] private bool _verboseDiagnostics=true;
  [SerializeField] private PresentationTimings _timings;
  private UnityHumanAgent _human;
  private LocalGameSession _session;
  private CardPlayabilityChecker _checker;
  private IReadOnlyList<Card> _humanHand;
  private GameState _lastState;
  private PresentationSequencer _sequencer;
  private Coroutine _forcedPickupCoroutine;
  private int _requestAnswered;
  private ForcedPickupOnce _forcedPickupOnce=new ForcedPickupOnce();
  public event Action<GameState> StateChanged;
  public event Action<IPlayerVisibleState> ExchangeStateChanged;
  public event Action<GameLogEntry> LogEntryReceived;
  public event Action<HumanRequestKind> HumanRequestPending;
  public event Action<GameRunOutcome> GameFinished;
  public event Action<GameState> CardsPlaced;
  public UnityHumanAgent Human {get{return _human;}}
  public ICardPlayabilityChecker PlayabilityChecker {get{return _checker;}}
  public IPlayerVisibleState VisibleState {get{return _human==null?null:_human.PendingState;}}
  public GameState LastState {get{return _lastState;}}
  public List<Card> HumanHandSorted {get{return HandSorter.Sort(_humanHand);}}
  public static List<Card> GetSortedHand(IPlayerVisibleState state){return HandSorter.Sort(state==null?null:state.SelfHand);}
  internal void Begin(){_checker=new CardPlayabilityChecker();_human=new UnityHumanAgent();var layout=new SeatLayout(new System.Random()).Create(_human,_checker);_session=new LocalGameSession(_human,layout);CreateSequencer();_session.Start(Environment.TickCount);Log("Session démarrée");}
  internal void Attach(UnityHumanAgent human,LocalGameSession session){_human=human;_session=session;CreateSequencer();}
  internal void Tick(){if(_sequencer!=null)_sequencer.Tick(Time.deltaTime);}
  private void Start(){Begin();}
  private void Update(){if(_session!=null){PreGameExchangeState exchange; if(_session.TryDequeueExchangeState(out exchange)){Action<IPlayerVisibleState> handler=ExchangeStateChanged;if(handler!=null)handler(PlayerVisibleState.FromExchange(exchange,HumanPlayerId));}}if(_sequencer!=null){if(Interlocked.Exchange(ref _requestAnswered,0)!=0)_sequencer.NotifyRequestAnswered();_sequencer.Tick(Time.deltaTime);}}
  private void CreateSequencer(){if(_session==null)return;var settings=_timings!=null?_timings.ToSettings():new PresentationTimingValues(1f,1f,1f);_sequencer=new PresentationSequencer(_session.Timeline,settings);_sequencer.StepPresented+=OnStepPresented;if(_human!=null)_human.RequestAnswered+=OnHumanRequestAnswered;}
  private void OnStepPresented(TimelineStep step){if(step==null)return;switch(step.Kind){case TimelineStepKind.SnapshotPlaced:_lastState=step.State;if(CardsPlaced!=null)CardsPlaced(step.State);break;case TimelineStepKind.SnapshotResolved:HandleStateChanged(step.State);break;case TimelineStepKind.Request:HandleHumanRequestPending(step.RequestKind);if(HumanRequestPending!=null)HumanRequestPending(step.RequestKind);break;case TimelineStepKind.Failure:Debug.LogException(step.Error);break;case TimelineStepKind.Log:if(LogEntryReceived!=null)LogEntryReceived(step.LogEntry);break;case TimelineStepKind.Finished:CancelForcedPickup();if(GameFinished!=null)GameFinished(_session==null?null:_session.Outcome);break;}}
  private void OnHumanRequestAnswered(){Interlocked.Exchange(ref _requestAnswered,1);}
  private void HandleStateChanged(GameState state){_lastState=state;if(state!=null&&state.Players.Count>0)_humanHand=PlayerVisibleState.From(state,state.Players[0].Id).SelfHand;if(state!=null&&state.GetFinishRank(HumanPlayerId).HasValue&&_sequencer!=null)_sequencer.IsFastMode=true;if(StateChanged!=null)StateChanged(state);}
  private void HandleHumanRequestPending(HumanRequestKind kind){CancelForcedPickup();_forcedPickupOnce=new ForcedPickupOnce();if(kind!=HumanRequestKind.Pickup||_human==null||!_human.PendingIsForced)return;Debug.Log("Aucune carte jouable");_forcedPickupCoroutine=StartCoroutine(SubmitForcedPickupAfterDelay());}
  private System.Collections.IEnumerator SubmitForcedPickupAfterDelay(){float delay=_timings!=null?_timings.ForcedPickupDelaySeconds:1f;yield return new WaitForSeconds(delay);_forcedPickupCoroutine=null;if(_human==null||_human.PendingRequest!=HumanRequestKind.Pickup||!_human.PendingIsForced||!_forcedPickupOnce.TryClaim())yield break;_human.SubmitPickup(true);}
  private void CancelForcedPickup(){if(_forcedPickupCoroutine==null)return;StopCoroutine(_forcedPickupCoroutine);_forcedPickupCoroutine=null;}
  private void OnDisable(){CancelForcedPickup();} private void OnDestroy(){CancelForcedPickup();if(_human!=null)_human.RequestAnswered-=OnHumanRequestAnswered;if(_session!=null)_session.Cancel();}
  private void Log(string message){if(_verboseDiagnostics)Debug.Log("[Gameplay] "+message);}
 }
}
