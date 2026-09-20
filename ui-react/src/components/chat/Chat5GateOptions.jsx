// Chat5a/Chat5b only: the row of 5 guardrail-gate checkboxes below the chat input.
// Order matches the real request pipeline: 500 Char -> Code Input -> LLM Input (pre-flight,
// AND semantics) -> Sys Prompt (inside the orchestrator's own instructions) -> LLM Output
// (post-flight, disables streaming for that turn when checked).
const GATES = [
  {
    key: 'maxLength',
    label: '500 Char',
    description:
      "Preorchestration - Deterministic Max Length. Blocks the request if the message is over 500 characters.",
  },
  {
    key: 'ruleInput',
    label: 'Code Input',
    description:
      'Preorchestration - Deterministic Input Check. A keyword/regex heuristic classifies the message as in/out of scope, no LLM call.',
  },
  {
    key: 'llmInput',
    label: 'LLM Input',
    description:
      'Preorchestration - LLM-based Input Check. A separate, cheap LLM call classifies the message as in/out of scope before the orchestrator runs.',
  },
  {
    key: 'systemPrompt',
    label: 'Sys Prompt',
    description:
      "Orchestration - Prompt-based Scope Guard. An instruction in the orchestrator's own system prompt telling it to only answer weather/location questions — no code enforces this, so it's the easiest gate to bypass.",
  },
  {
    key: 'llmOutput',
    label: 'LLM Output',
    description:
      "Postorchestration - LLM-based Output Check. A separate LLM call classifies the full response before it's shown; requires buffering the complete response first, so token streaming is disabled for that turn.",
  },
];

function Chat5GateOptions({ state, onChange }) {
  return (
    <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground">
      {GATES.map((gate) => (
        <label key={gate.key} className="flex cursor-help items-center gap-1.5" title={gate.description}>
          <input
            type="checkbox"
            checked={state[gate.key]}
            onChange={(event) => onChange(gate.key, event.target.checked)}
          />
          {gate.label}
        </label>
      ))}
    </div>
  );
}

export default Chat5GateOptions;
