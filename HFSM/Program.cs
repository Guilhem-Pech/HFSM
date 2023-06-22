using HFSM;

// Exemple
HFSMBuilder<States, Events> myFsm = new HFSMBuilder<States, Events>();
myFsm.AddState(States.root);
{
    myFsm.AddState(States.a, States.root, () => Console.WriteLine("OnEnter a"));
    {
        myFsm.AddState(States.c, States.a, () => Console.WriteLine("OnEnter c"));
        {
            myFsm.AddTransition(States.b, States.a, Events.Trigger);
        }
        myFsm.AddTransition(States.a, States.b, () => false);
    }
    myFsm.AddState(States.b, States.root, () => Console.WriteLine("OnEnter b"));
    {
        myFsm.AddTransition(States.b, States.a);
    }
}

HFSM<States, Events> myFSM = myFsm.Build();
myFSM.Start();
myFSM.Update();

enum States
{
    root,
    a,
    b,
    c
};

enum Events
{
    Invalid,
    Trigger
}