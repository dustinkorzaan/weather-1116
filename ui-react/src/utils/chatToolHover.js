export const TOOL_HOVER_CLOSE_DELAY_MS = 200;

export function formatToolHoverText({ toolArguments, toolResult, running } = {}) {
  const sections = [];
  if (toolArguments) {
    sections.push(`Arguments\n${toolArguments}`);
  }
  if (toolResult) {
    sections.push(`Result\n${toolResult}`);
  }
  if (sections.length === 0) {
    return running ? 'Waiting for tool output…' : '';
  }
  return sections.join('\n\n');
}
