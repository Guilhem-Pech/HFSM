# HFSM Library

HFSM (Hierarchical Finite State Machine) is a simple and flexible library for building state machines in C#. The library provides an easy-to-use API for defining states, transitions, and hierarchical relationships between states.

## Features

- Define states with custom behavior (OnEnter, OnUpdate, OnExit)
- Define transitions based on events or guard conditions
- Hierarchical state organization
- State machine builder for easy construction and configuration
- Event-driven state updates

## Usage

To use the HFSM library, follow these steps:

1. Define your state and event enums.
2. Create a new `HFSMBuilder` instance, passing in the state and event enums as generic arguments.
3. Add states to the builder using the `AddState` method.
4. Define transitions between states using the `AddTransition` method.
5. Build the state machine using the `Build` method.
6. Start the state machine with the `Start` method.
7. Update the state machine using the `Update` method.

Here's an example:

```csharp
// Define your state and event enums
public enum MyState { Root, Idle, Walk, Run }
public enum MyEvent { StartWalking, StartRunning, Stop }

// Create a new HFSMBuilder instance
var builder = new HFSMBuilder<MyState, MyEvent>();

// Add states to the builder
builder.AddState(MyState.Movement);
builder.AddState(MyState.Idle, MyState.Movement);
builder.AddState(MyState.Walk, MyState.Movement);
builder.AddState(MyState.Run, MyState.Movement);

// Define transitions between states
builder.AddTransition(MyState.Idle, MyState.Walk, MyEvent.StartWalking);
builder.AddTransition(MyState.Walk, MyState.Run, MyEvent.StartRunning);
builder.AddTransition(MyState.Run, MyState.Idle, MyEvent.Stop);

// Build the state machine
var hfsm = builder.Build();

// Start the state machine
hfsm.Start();

// Update the state machine
hfsm.Update();
```

## Contributing

Contributions to the HFSM library are welcome! To contribute, please follow these steps:

1. Fork the repository.
2. Create a new branch for your feature or bugfix.
3. Make your changes and commit them to your branch.
4. Create a pull request, providing a detailed description of your changes.

Before submitting your pull request, please make sure to test your changes and provide appropriate unit tests if applicable.

## Visualizing the State Machine Graph

To visualize the state machine graph, you can generate a DOT language representation using the `GenerateDotRepresentation` method in the `HFSMBuilder` class. This method should be invoked after the state machine is built. The method generates a DOT file with a specified name that can be visualized using any Graphviz viewer. Note that the file name is passed as a parameter to the `GenerateDotRepresentation()` method.

Here's an example:

```csharp
// Build the state machine
var hfsm = builder.Build();

// Generate the DOT representation
bool success = hfsm.GenerateDotRepresentation("HFSM.dot");
```

If the `GenerateDotRepresentation()` method returns `true`, it means the operation was successful and you can then open the "HFSM.dot" file with a Graphviz viewer to visualize the state machine graph. If it returns `false`, it means the operation failed. This could be due to an exception during file writing or if the state machine is empty. In case of an exception, check the console for the error message. If the state machine is empty, ensure that you have added states and transitions before invoking the `GenerateDotRepresentation()` method. Some popular Graphviz viewers include [WebGraphviz](http://www.webgraphviz.com/) and [Graphviz Visual Studio Code extension](https://marketplace.visualstudio.com/items?itemName=EFanZh.graphviz-preview).
