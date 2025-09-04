using System;

namespace Prediction
{
    /// <summary>
    /// A predictor that extends <see cref="InputStatePredicter{TState, TInput}"/> with support for smoothing between states.
    /// Provides methods to interpolate between predicted and corrected states for smoother transitions.
    /// </summary>
    /// <typeparam name="TState">The type of the state, must be unmanaged.</typeparam>
    /// <typeparam name="TInput">The type of the input, must be unmanaged.</typeparam>
    public class SmoothingInputStatePredicter<TState, TInput> : InputStatePredictor<TState, TInput>
        where TState : unmanaged where TInput : unmanaged
    {
        private readonly Func<TState, TState, float, TState> smoother;
        private TState lastCorrectedState;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmoothingInputStatePredicter{TState, TInput}"/> class.
        /// </summary>
        /// <param name="bufferHistory">The size of the state and input history buffers.</param>
        /// <param name="oneFrameWaitInputApply">If true, applies inputs with a one-frame delay, storing the current state before applying the input.</param>
        /// <param name="startState">The initial state of the predictor.</param>
        /// <param name="applyInputFunc">Function to apply an input to a state, producing a new state.</param>
        /// <param name="stateComparerFunc">Function to compare two states, determining if correction is needed.</param>
        /// <param name="stateCorrectorFunc">Optional function to merge an authoritative state with the predicted state during correction. Defaults to using the authoritative state fully.</param>
        /// <param name="smootherFunc">Function to interpolate between two states using a factor (0 to 1). Required for smoothing.</param>
        public SmoothingInputStatePredicter(int bufferHistory, bool oneFrameWaitInputApply, TState startState,
            Func<TState, TInput, TState> applyInputFunc, Func<TState, TState, bool> stateComparerFunc,
            Func<TState, TState, float, TState> smootherFunc,
            Func<TState, TState, TState> stateCorrectorFunc = null)
            : base(bufferHistory, oneFrameWaitInputApply, startState, applyInputFunc, stateComparerFunc, stateCorrectorFunc)
        {
            smoother = smootherFunc;
            lastCorrectedState = startState;
        }

        /// <summary>
        /// Processes an authoritative state, correcting the predicted state if necessary and updating the last corrected state for smoothing.
        /// </summary>
        /// <param name="serverState">The authoritative state received.</param>
        /// <param name="serverTick">The tick associated with the authoritative state.</param>
        /// <returns>The corrected state if a correction was needed; otherwise, the authoritative state.</returns>
        public override TState StateReceived(TState serverState, int serverTick)
        {
            var result = base.StateReceived(serverState, serverTick);
            lastCorrectedState = result;
            return result;
        }

        /// <summary>
        /// Interpolates between the current predicted state and a target state using the smoother function.
        /// </summary>
        /// <param name="targetState">The target state to smooth towards.</param>
        /// <param name="t">The interpolation factor (0 to 1, where 0 is the current state and 1 is the target state).</param>
        /// <returns>The smoothed state.</returns>
        public TState Smooth(TState targetState, float t)
        {
            var (currentState, _) = GetCurrentState();
            return smoother(currentState, targetState, t);
        }

        /// <summary>
        /// Gets a smoothed version of the current state, interpolating towards the last corrected state.
        /// </summary>
        /// <param name="t">The interpolation factor (0 to 1, where 0 is the current state and 1 is the last corrected state).</param>
        /// <returns>The smoothed state.</returns>
        public TState GetSmoothedState(float t)
        {
            var (currentState, _) = GetCurrentState();
            return smoother(currentState, lastCorrectedState, t);
        }
    }
}
