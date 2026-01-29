namespace UIFramework
{
    public enum AnimationState
    {
        Normal, // the default state, this is being defined from the style block directly instead of in the animation block
        Enter, // when show it will go to this state first then transition to normal state
        Exit, // when hide it will transition to this state
        Initial, // when repeat is enable, after enter it will go to this state and transition to animate
        Animate, // when repeat is enable, it will transition to this and back to initial, when it is over it will go back to normal 
        Hover, // when hovering
        Press, // when press
        Check // when check
    }
}