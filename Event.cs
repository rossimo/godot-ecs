using Ecs;

public static class Event
{
    public static State System(State state, string id, Component ev)
    {
        return state.With(id, ev);
    }
}