using System.Text;

namespace HFSM;

public interface IState {}
public class Transition<TEvent> where TEvent: struct, Enum
{
    private readonly IState m_to;
    private readonly Func<bool> m_guard = () => true;
    private readonly TEvent? m_event = null;
	
    public Transition(IState _to)
    {
        m_to = _to ?? throw new ArgumentNullException(nameof(_to));;
    }
    
    public Transition(IState _to, TEvent? _event = null, Func<bool>? _guard = null) : this(_to)
    {
	    m_to = _to ?? throw new ArgumentNullException(nameof(_to));
	    m_guard = _guard ?? m_guard;
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
    public override string ToString()
    {
	    StringBuilder builder = new StringBuilder();

	    builder.Append( $"To: {m_to} " );
	    if ( m_event.HasValue )
	    {
		    builder.Append( $"Event: {m_event} " );
	    }
	    builder.Append( $"Guard Result: {m_guard()} " );
	    return builder.ToString();
    }
}
public class State<TName, TEvent> : IState where TEvent: struct, Enum where TName: Enum
{
    private static readonly Action? NoActivity = () => { };
    public State(TName _name)
    {
        Name = _name;
    }
    
    public State(TName _name, State<TName, TEvent>? _root = null, Action? _onEnterAction = null, Action? _onUpdateAction = null,
	    Action? _onExitAction = null)
	    : this(_name)
    {
	    if (_onEnterAction != null) m_onEnterAction = _onEnterAction;
	    if (_onUpdateAction != null) m_onUpdateAction = _onUpdateAction;
	    if (_onExitAction != null) m_onExitAction = _onExitAction;
	    
	    m_parentState = _root;
	    _root?.AddChild(this);
    }

    public State(TName _name, Action? _onEnterAction = null, Action? _onUpdateAction = null,
	    Action? _onExitAction = null)
	    : this(_name)
    {
	    if (_onEnterAction != null) m_onEnterAction = _onEnterAction;
	    if (_onUpdateAction != null) m_onUpdateAction = _onUpdateAction;
	    if (_onExitAction != null) m_onExitAction = _onExitAction;
    }
	

    public TName Name { get; private set; }

    private readonly Action? m_onEnterAction = NoActivity;
    private readonly Action? m_onUpdateAction = NoActivity;
    private readonly Action? m_onExitAction = NoActivity;

    private State<TName, TEvent>? m_parentState = null;
    private List<State<TName, TEvent>> m_children = new();
    private List<Transition<TEvent>> m_transitions = new();

    public void AddTransition(Transition<TEvent> _transition)
    {
        m_transitions.Add(_transition);
    }
    public State<TName, TEvent>? CheckTransitions(TEvent? _fsmEvent, out Transition<TEvent>? _matchedTransition )
    {
	    _matchedTransition = null;
        foreach (Transition<TEvent> transition in m_transitions)
        {
	        if ( transition.MatchConditions( _fsmEvent ) )
	        {
		        _matchedTransition = transition;
		        return transition.Destination() as State<TName, TEvent>;
	        }
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

    public void OnEnter() { m_onEnterAction?.Invoke(); }
    public void OnUpdate() { m_onUpdateAction?.Invoke(); }
    public void OnExit() { m_onExitAction?.Invoke(); }

    public override string ToString()
    {
	    return $"[{Name}]";
    }

    public IList<Transition<TEvent>> GetTransitions()
    {
        return m_transitions;
    }
}

public class HFSMBuilder<TName, TEvent> where TEvent : struct, Enum where TName : Enum
{
    private readonly Dictionary<TName, State<TName, TEvent>> m_states = new();
    private readonly List<TransitionInfo> m_transitionInfos = new();
    private readonly Dictionary<TName, TName> m_stateParents = new();

    private class TransitionInfo
    {
        public TName From { get; }

        public TName To { get; }

        public Func<bool>? Guard { get; }

        public TEvent? Trigger { get; } = null;
		
        public TransitionInfo(TName _from, TName _to, Func<bool>? _guard = null, TEvent? _event = null)
        {
            From = _from;
            To = _to;
            Guard = _guard;
            Trigger = _event;
        }
        
        public TransitionInfo(TName _from, TName _to, TEvent _event)
        {
            From = _from;
            To = _to;
            Trigger = _event;
        }
    }
    
    public HFSMBuilder<TName, TEvent> AddState(TName _name, Action? _onEnterAction = null, Action? _onUpdateAction = null, Action? _onExitAction = null)
    {
        m_stateParents.Add(_name, _name);
        m_states.Add(_name, new State<TName, TEvent>(_name, _onEnterAction, _onUpdateAction, _onExitAction));
        return this;
    }
    
    public HFSMBuilder<TName, TEvent> AddState(TName _name, TName _parentName, Action? _onEnterAction = null, Action? _onUpdateAction = null, Action? _onExitAction = null)
    {
        if ( m_states.ContainsKey( _name ) )
        {
	        throw new ArgumentException( $"State already defined {_name}" );
        }
        
        m_stateParents.Add(_name, _parentName);
        m_states.Add(_name, new State<TName, TEvent>(_name, _onEnterAction, _onUpdateAction, _onExitAction));
        return this;
    }
    
    public HFSMBuilder<TName, TEvent> AddTransition(TName _from, TName _to, TEvent? _event = null)
    {
        m_transitionInfos.Add(new TransitionInfo(_from, _to,null, _event));
        return this;
    }
	
    public HFSMBuilder<TName, TEvent> AddTransition(TName _from, TName _to, Func<bool> _guard, TEvent? _event = null)
    {
        m_transitionInfos.Add(new TransitionInfo(_from, _to, _guard, _event));
        return this;
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
            m_states.TryGetValue( transitionInfo.From, out State<TName, TEvent>? from );
            m_states.TryGetValue( transitionInfo.To, out State<TName, TEvent>? to );

            if ( from == null )
            {
                Console.WriteLine( $"Cannot add transition {transitionInfo.From} => {transitionInfo.To} - {transitionInfo.From} state is not defined" );
	            continue;
            }
            
            if ( to == null )
            {
                Console.WriteLine( $"Cannot add transition {transitionInfo.From} => {transitionInfo.To} - {transitionInfo.To} state is not defined" );
	            continue;
            }
            
            from.AddTransition(new Transition<TEvent>(to, transitionInfo.Trigger, transitionInfo.Guard));
        }
        return new HFSM<TName, TEvent>(initial ?? throw new InvalidOperationException("No root state detected !"));
    }
}
public class HFSM<TName, TEvent> where TEvent: struct, Enum where TName: Enum 
{
    private readonly State<TName, TEvent> m_initialState;
    private State<TName, TEvent>? m_currentState;

    private readonly Queue<TEvent?> m_eventToTreat = new();
    
    public bool EnableDebugLog { set; get; } = false;
    
    private Transition<TEvent>? m_lastTransition;

    private void LogTransition(State<TName, TEvent>? _oldState, State<TName, TEvent>? _newState)
    {
	    if (EnableDebugLog)
	    {
		    m_lastTransition.EnableDebugLog = EnableDebugLog;
		    Console.WriteLine($"[HFSM] Transition: {_oldState} -> {_newState}, Reason: {m_lastTransition}");
	    }
	    // Add thorough testing for the change where the `EnableDebugLog` property of the `m_lastTransition` object is set
    }
    
    public string GetDebugCurrentStateName()
    {
	    StringBuilder stateNamesBuilder = new StringBuilder();

	    foreach (var state in GetActiveStatesHierarchy())
	    {
		    stateNamesBuilder.Append('.');
		    stateNamesBuilder.Append(state.Name);
	    }
 
	    string names = stateNamesBuilder.ToString();

	    return names.StartsWith(".") ? names.TrimStart('.') : names;
    }

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
            State<TName, TEvent>? destinationState = state.CheckTransitions(receivedEvent, out m_lastTransition);
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
        
        LogTransition( m_currentState, nextState );
        m_currentState = nextState;
        
        List<State<TName, TEvent>> enteringStates = futureStatesHierarchy.Except(activeStatesHierarchy).ToList();
        foreach (State<TName, TEvent> state in enteringStates)
        {
            state.OnEnter();
        }
    }
    public void Stop()
    {
	    m_lastTransition = null;
        ChangeActiveState(null);
    }
    
    ~HFSM()
    {
	    if(m_currentState != null)
			Stop();
    }

    public State<TName, TEvent> GetRootState()
    {
        return m_initialState;
    }
}