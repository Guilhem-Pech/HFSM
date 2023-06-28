using System.Diagnostics;

namespace HFSM;

public interface IState {}
public class Transition<TEvent> where TEvent: struct, Enum
{
    private IState m_to;
    private Func<bool> m_guard = () => true;
    private TEvent? m_event = null;

  

    public Transition(IState _to)
    {
        m_to = _to;
    }
    public Transition(IState _to, TEvent _event) : this(_to)
    {
        m_event = _event;
    }
    public Transition(IState _to, Func<bool> _guard) : this(_to)
    {
        m_guard = _guard;
    }
    public Transition(IState _to, TEvent? _event, Func<bool> _guard) : this(_to)
    {
        m_guard = _guard;
        m_event = _event;
    }

    public bool MatchConditions(TEvent? _fsmEvent)
    {
        return m_guard() && m_event.Equals(_fsmEvent);
    }
    public IState Destination()
    {
        return m_to;
    }
}
public class State<TName, TEvent> : IState where TEvent: struct, Enum where TName: Enum
{
    private static Action? _noActivity = () => { };
    
    public State(TName _name)
    {
        m_name = _name;
    }
    
    public State(TName _name, State<TName, TEvent>? _root = null, Action? _onEnterAction = null, Action? _onUpdateAction = null,
        Action? _onExitAction = null)
        : this(_name)
    {
        m_parentState = _root;
        _root?.AddChild(this);
        
        if (_onEnterAction != null) m_onEnterAction = _onEnterAction;
        if (_onUpdateAction != null) m_onUpdateAction = _onUpdateAction;
        if (_onExitAction != null) m_onExitAction = _onExitAction;
    }
    
    public State(TName _name, Action? _onEnterAction = null, Action? _onUpdateAction = null,
        Action? _onExitAction = null)
        : this(_name)
    {
        if (_onEnterAction != null) m_onEnterAction = _onEnterAction;
        if (_onUpdateAction != null) m_onUpdateAction = _onUpdateAction;
        if (_onExitAction != null) m_onExitAction = _onExitAction;
    }

    private TName m_name;

    private Action? m_onEnterAction = _noActivity;
    private Action? m_onUpdateAction = _noActivity;
    private Action? m_onExitAction = _noActivity;

    private State<TName, TEvent>? m_parentState = null;
    private List<State<TName, TEvent>> m_children = new();
    private List<Transition<TEvent>> m_transitions = new();

    public void AddTransition(Transition<TEvent> _transition)
    {
        m_transitions.Add(_transition);
    }
    public State<TName, TEvent>? CheckTransitions(TEvent? _fsmEvent)
    {
        foreach (Transition<TEvent> transition in m_transitions)
        {
            if (transition.MatchConditions(_fsmEvent))
                return transition.Destination() as State<TName, TEvent>;
        }
        return null;
    }
    
    public List<State<TName, TEvent>> GetChildren()
    {
        return m_children;
    }
    private void AddChild(State<TName, TEvent> _state) { m_children.Add(_state); }
    
    public void SetParent(State<TName,TEvent> _state)
    {
        m_parentState = _state;
        m_parentState.AddChild(this);
    }
    
    public State<TName, TEvent>? GetParent()
    {
        return m_parentState;
    }

    public TName Name { get => m_name; set => m_name = value; }
    public void OnEnter() { m_onEnterAction?.Invoke(); }
    public void OnUpdate() { m_onUpdateAction?.Invoke(); }
    public void OnExit() { m_onExitAction?.Invoke(); }

    
}

public class HFSMBuilder<TName, TEvent> where TEvent : struct, Enum where TName : Enum
{
    private Dictionary<TName, State<TName, TEvent>> m_states = new();
    private List<TransitionInfo> m_transitionInfos = new();
    private Dictionary<TName, TName> m_stateParents = new();

    private class TransitionInfo
    {
        private TEvent? m_trigger = null;

        public TName From { get; set; }

        public TName To { get; set; }

        public Func<bool>? Guard { get; set; }

        public TEvent? Trigger
        {
            get => m_trigger;
            set => m_trigger = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        public TransitionInfo(TName _from, TName _to)
        {
            From = _from;
            To = _to;
        }

        public TransitionInfo(TName _from, TName _to, Func<bool>? _guard)
        {
            From = _from;
            To = _to;
            Guard = _guard;
        }
        
        public TransitionInfo(TName _from, TName _to, Func<bool>? _guard, TEvent _event)
        {
            From = _from;
            To = _to;
            Guard = _guard;
            m_trigger = _event;
        }
        
        public TransitionInfo(TName _from, TName _to, TEvent _event)
        {
            From = _from;
            To = _to;
            m_trigger = _event;
        }
    }
    
    public void AddState(TName _name, Action? _onEnterAction = null, Action? _onUpdateAction = null, Action? _onExitAction = null)
    {
        m_stateParents.Add(_name, _name);
        m_states.Add(_name, new State<TName, TEvent>(_name, _onEnterAction, _onUpdateAction, _onExitAction));
    }
    
    public void AddState(TName _name, TName _parentName, Action? _onEnterAction = null, Action? _onUpdateAction = null, Action? _onExitAction = null)
    {
        Debug.Assert(!m_states.ContainsKey(_name), $"State already defined {_name}");
        
        m_stateParents.Add(_name, _parentName);
        m_states.Add(_name, new State<TName, TEvent>(_name, _onEnterAction, _onUpdateAction, _onExitAction));
    }
    
    public void AddTransition(TName _from, TName _to)
    {
        m_transitionInfos.Add(new TransitionInfo(_from, _to));
    }
    public void AddTransition(TName _from, TName _to, TEvent _event)
    {
        m_transitionInfos.Add(new TransitionInfo(_from, _to, _event));
    }
    
    public void AddTransition(TName _from, TName _to, Func<bool> _guard)
    {
        m_transitionInfos.Add(new TransitionInfo(_from, _to, _guard));
    }
    
    public void AddTransition(TName _from, TName _to, Func<bool> _guard, TEvent _event)
    {
        m_transitionInfos.Add(new TransitionInfo(_from, _to, _guard, _event));

    }

    public HFSM<TName, TEvent> Build()
    {
        State<TName, TEvent>? initial = null;
        
        foreach (var (name,state) in m_states)
        {
            if (!Equals(m_stateParents[name], name))
            {
                state.SetParent(m_states[m_stateParents[name]]);
            }
            else if (initial == null)
            {
                initial = state;
            }
        }
        
        foreach (var transitionInfo in m_transitionInfos)
        {
            State<TName, TEvent> from = m_states[transitionInfo.From];
            State<TName, TEvent> to = m_states[transitionInfo.To];
            
            if (transitionInfo is { Trigger: not null, Guard: not null })
            {
                from.AddTransition(new Transition<TEvent>(to, transitionInfo.Trigger.Value, transitionInfo.Guard));
            }
            else if(transitionInfo.Trigger.HasValue)
            {
                from.AddTransition(new Transition<TEvent>(to, transitionInfo.Trigger.Value));
            }
            else if(transitionInfo.Guard != null)
            {
                from.AddTransition(new Transition<TEvent>(to, transitionInfo.Guard));
            }
            else
            {
                from.AddTransition(new Transition<TEvent>(to));
            }
        }

        return new HFSM<TName, TEvent>(initial ?? throw new InvalidOperationException("No root state detected !"));
    }
}
public class HFSM<TName, TEvent> where TEvent: struct, Enum where TName: Enum
{
    private State<TName, TEvent> m_initialState;
    private State<TName, TEvent>? m_currentState;

    private Queue<TEvent?> m_eventToTreat = new();
    
    public HFSM(State<TName, TEvent> _initial)
    {
        m_initialState = _initial;
    }
    
    public List<State<TName, TEvent>> GetActiveStatesHierarchy()
    {
        return m_currentState == null ? new List<State<TName, TEvent>>() : GetStatesHierarchy(m_currentState);
    }

    private List<State<TName, TEvent>> GetStatesHierarchy(State<TName, TEvent> _root)
    {
        List<State<TName, TEvent>> activeHierarchy = new List<State<TName, TEvent>>();
        
        for (State<TName, TEvent>? parent = _root; parent != null; parent = parent.GetParent())
        {
            activeHierarchy.Insert(0, parent);
        }
        
        return activeHierarchy;
    }

    private static State<TName, TEvent> GetFirstLeaf(State<TName, TEvent> _state)
    {
        State<TName, TEvent> returnValue = _state;
        while (returnValue.GetChildren().Count > 0)
        {
            returnValue = returnValue.GetChildren().First();
        }
        return returnValue;
    }
   
    public void Start()
    {
        ChangeActiveState(m_initialState);
    }
    
    public void Update()
    {
        if (m_currentState == null)
        {
            Start();
            return;
        }

        m_eventToTreat.TryDequeue(out var receivedEvent);
        
        foreach (State<TName, TEvent> state in GetActiveStatesHierarchy())
        {
            State<TName, TEvent>? destinationState = state.CheckTransitions(receivedEvent);
            if (destinationState != null)
            {
                ChangeActiveState(destinationState);
                break;
            }
            state.OnUpdate();
        }
    }
    
    public void SendEvent(TEvent _trigger)
    {
        m_eventToTreat.Enqueue(_trigger);
    }

    private void ChangeActiveState(State<TName, TEvent>? _newState)
    {
        State<TName, TEvent>? nextState = _newState != null ? GetFirstLeaf(_newState) : null;
        
        HashSet<State<TName, TEvent>> activeStatesHierarchy = new HashSet<State<TName, TEvent>>(GetActiveStatesHierarchy());
        HashSet<State<TName, TEvent>> futureStatesHierarchy = nextState != null ? new HashSet<State<TName, TEvent>>(GetStatesHierarchy(nextState)) : new ();
        
        List<State<TName, TEvent>> exitingStates = activeStatesHierarchy.Except(futureStatesHierarchy).ToList();
        for (int index = exitingStates.Count - 1; index >= 0; --index)
        {
            exitingStates[index].OnExit();
        }
        
        m_currentState = nextState;
        
        List<State<TName, TEvent>> enteringStates = futureStatesHierarchy.Except(activeStatesHierarchy).ToList();
        foreach (State<TName, TEvent> state in enteringStates)
        {
            state.OnEnter();
        }
    }
    public void Stop()
    {
        ChangeActiveState(null);
    }
    
    ~HFSM()
    {
        Stop();
    }
}