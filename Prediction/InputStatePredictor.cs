using System;

namespace Prediction
{
    /// <summary>
    /// A generic predictor that applies inputs to a state and corrects predictions based on authoritative states.
    /// Maintains a history of states and inputs for accurate state replay during correction.
    /// </summary>
    /// <typeparam name="TState">The type of the state, must be unmanaged.</typeparam>
    /// <typeparam name="TInput">The type of the input, must be unmanaged.</typeparam>
    public class InputStatePredictor<TState, TInput> where TState : unmanaged where TInput : unmanaged
    {
        private readonly CircularBuffer<TState> stateHistory;
        private readonly CircularBuffer<TInput> inputHistory;
        private readonly bool isOneFrameDelayed;

        private TState currentState;
        private int currentTick = 0;

        private readonly Func<TState, TInput, TState> applyInput;
        private readonly Func<TState, TState, bool> stateComparer;
        private readonly Func<TState, TState, TState> stateCorrector;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputStatePredictor{TState, TInput}"/> class.
        /// </summary>
        /// <param name="bufferHistory">The size of the state and input history buffers.</param>
        /// <param name="oneFrameWaitInputApply">If true, applies inputs with a one-frame delay, storing the current state before applying the input.</param>
        /// <param name="startState">The initial state of the predictor.</param>
        /// <param name="applyInputFunc">Function to apply an input to a state, producing a new state.</param>
        /// <param name="stateComparerFunc">Function to compare two states, determining if correction is needed.</param>
        /// <param name="stateCorrectorFunc">Optional function to merge an authoritative state with the predicted state during correction. Defaults to using the authoritative state fully.</param>
        public InputStatePredictor(int bufferHistory, bool oneFrameWaitInputApply, TState startState,
            Func<TState, TInput, TState> applyInputFunc, Func<TState, TState, bool> stateComparerFunc,
            Func<TState, TState, TState> stateCorrectorFunc = null)
        {
            stateHistory = new(bufferHistory);
            inputHistory = new(bufferHistory);
            currentState = startState;
            applyInput = applyInputFunc;
            isOneFrameDelayed = oneFrameWaitInputApply;
            stateComparer = stateComparerFunc;
            stateCorrector = stateCorrectorFunc ?? ((server, client) => server);

            stateHistory.Add(currentState, 0);
        }

        /// <summary>
        /// Applies an input to the current state and updates the state and input history.
        /// </summary>
        /// <param name="input">The input to apply.</param>
        /// <param name="tick">The tick associated with the input.</param>
        public void ApplyInput(TInput input, int tick)
        {
            inputHistory.Add(input, tick);

            if (isOneFrameDelayed)
            {
                stateHistory.Add(currentState, tick);
                currentState = ApplyInputInternal(currentState, input, tick);
            }
            else
            {
                currentState = ApplyInputInternal(currentState, input, tick);
                stateHistory.Add(currentState, tick);
            }

            currentTick = tick;
        }

        /// <summary>
        /// Processes an authoritative state, correcting the predicted state if necessary by replaying inputs.
        /// </summary>
        /// <param name="serverState">The authoritative state received.</param>
        /// <param name="serverTick">The tick associated with the authoritative state.</param>
        /// <returns>The corrected state if a correction was needed; otherwise, the authoritative state.</returns>
        public virtual TState StateReceived(TState serverState, int serverTick)
        {
            if (!stateComparer(currentState, serverState))
            {
                TState corrected = stateCorrector(serverState, currentState);
                stateHistory.Add(corrected, serverTick);

                for (int i = serverTick + 1; i <= currentTick; i++)
                {
                    var input = inputHistory.Get(i);
                    corrected = ApplyInputInternal(corrected, input, i);
                    stateHistory.Add(corrected, i);
                }

                currentState = corrected;
                return corrected;
            }

            return serverState;
        }

        /// <summary>
        /// Retrieves the state at a specific tick from the history.
        /// </summary>
        /// <param name="tick">The tick for which to retrieve the state.</param>
        /// <returns>The state at the specified tick.</returns>
        public TState GetStateAtTick(int tick)
        {
            return stateHistory.Get(tick);
        }

        /// <summary>
        /// Gets the current predicted state and its associated tick.
        /// </summary>
        /// <returns>A tuple containing the current state and its tick.</returns>
        public (TState state, int tick) GetCurrentState()
        {
            return (currentState, currentTick);
        }

        /// <summary>
        /// Applies an input to a state for a given tick, producing a new state.
        /// </summary>
        /// <param name="state">The state to which the input is applied.</param>
        /// <param name="input">The input to apply.</param>
        /// <param name="tick">The tick associated with the input.</param>
        /// <returns>The new state after applying the input.</returns>
        protected virtual TState ApplyInputInternal(TState state, TInput input, int tick)
        {
            return applyInput.Invoke(state, input);
        }
    }
}
