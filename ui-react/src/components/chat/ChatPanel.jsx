import { useLayoutEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import SafeGfmMarkdown from '../markdown/SafeGfmMarkdown';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { findLastIndex } from '../../utils/array';
import { formatToolHoverText, TOOL_HOVER_CLOSE_DELAY_MS } from '../../utils/chatToolHover';
import { streamChatMessage } from '../../utils/chatStream';
import { formatChatUsageChip, formatChatUsageDetails } from '../../utils/chatUsage';
import Chat5GateOptions from './Chat5GateOptions';

const TAB_CONFIG = [
  {
    id: 'Chat1a',
    label: 'Chat1a',
    shortLabel: '1a',
    description: 'Responses API · Local Loops · Like Foundry Console V3',
    endpoint: '/Chat1a/messages',
  },
  {
    id: 'Chat1b',
    label: 'Chat1b',
    shortLabel: '1b',
    description: 'Responses API · Remote MCP · Like Foundry Console V4',
    endpoint: '/Chat1b/messages',
  },
  {
    id: 'Chat2a',
    label: 'Chat2a',
    shortLabel: '2a',
    description: 'Agent Framework · Local Loops · Like Foundry Console V3',
    endpoint: '/Chat2a/messages',
  },
  {
    id: 'Chat2b',
    label: 'Chat2b',
    shortLabel: '2b',
    description: 'Agent Framework · Remote MCP · Like Foundry Console V4',
    endpoint: '/Chat2b/messages',
  },
  {
    id: 'Chat3',
    label: 'Chat3',
    shortLabel: '3',
    description: 'Hosted Foundry agent · Like Foundry Console V5',
    endpoint: '/Chat3/messages',
  },
  {
    id: 'Chat4a',
    label: 'Chat4a',
    shortLabel: '4a',
    description: 'Agent Framework · Local Loops · Multi-agent · Orchestration agent delegates to Geo and NonAI Weather Agents (invalid Token Counts)',
    endpoint: '/Chat4a/messages',
  },
  {
    id: 'Chat4b',
    label: 'Chat4b',
    shortLabel: '4b',
    description: 'Agent Framework · Remote MCP · Multi-agent · Orchestration agent delegates to Geo and NonAI Weather Agents (invalid Token Counts)',
    endpoint: '/Chat4b/messages',
  },
  {
    id: 'Chat5a',
    label: 'Chat5a',
    shortLabel: '5a',
    description: 'Agent Framework · Local Loops · Multi-agent · Orchestration agent delegates to Geo and NonAI Weather Agents · 5 Guardrailed (invalid Token Counts)',
    endpoint: '/Chat5a/messages',
    hasGates: true,
  },
  {
    id: 'Chat5b',
    label: 'Chat5b',
    shortLabel: '5b',
    description: 'Agent Framework · Remote MCP · Multi-agent · Orchestration agent delegates to Geo and NonAI Weather Agents · 5 Guardrailed (invalid Token Counts)',
    endpoint: '/Chat5b/messages',
    hasGates: true,
  },
];

const GATE_DEFAULTS = {
  maxLength: true,
  ruleInput: true,
  llmInput: true,
  systemPrompt: true,
  llmOutput: true,
};

const MESSAGE_CLASSES = {
  user: 'h-max min-h-min shrink-0 self-end max-w-[85%] overflow-visible rounded-2xl bg-primary px-3 py-2 text-primary-foreground whitespace-pre-wrap',
  assistant: 'h-max min-h-min shrink-0 self-start max-w-[85%] overflow-visible rounded-2xl border border-border bg-muted px-3 py-2 text-foreground',
  tool: 'h-max min-h-min shrink-0 self-center text-xs text-muted-foreground',
  error: 'h-max min-h-min shrink-0 w-full rounded-md bg-destructive/15 px-3 py-2 text-destructive',
  blocked: 'h-max min-h-min shrink-0 w-full rounded-md bg-amber-500/15 px-3 py-2 text-amber-600 dark:text-amber-400',
};

function messageClasses(entry) {
  if (entry.role === 'assistant' && !entry.streaming) {
    return `${MESSAGE_CLASSES.assistant} chat-markdown`;
  }
  if (entry.role === 'assistant') {
    return `${MESSAGE_CLASSES.assistant} whitespace-pre-wrap`;
  }
  return MESSAGE_CLASSES[entry.role] ?? MESSAGE_CLASSES.assistant;
}

function scrollElementToBottom(element) {
  if (!element) {
    return;
  }

  element.scrollTop = element.scrollHeight;
}

function ToolChip({ content, details, className }) {
  const chipRef = useRef(null);
  const tooltipRef = useRef(null);
  const hideTimerRef = useRef(null);
  const [open, setOpen] = useState(false);
  const [coords, setCoords] = useState({ top: 0, left: 0 });

  const cancelHide = () => {
    if (hideTimerRef.current !== null) {
      window.clearTimeout(hideTimerRef.current);
      hideTimerRef.current = null;
    }
  };

  const updatePosition = () => {
    const node = chipRef.current;
    if (!node) {
      return;
    }

    const rect = node.getBoundingClientRect();
    setCoords({ top: rect.bottom, left: rect.left + rect.width / 2 });
  };

  const show = () => {
    cancelHide();
    updatePosition();
    setOpen(true);
  };

  const scheduleHide = () => {
    cancelHide();
    hideTimerRef.current = window.setTimeout(() => {
      hideTimerRef.current = null;
      setOpen(false);
    }, TOOL_HOVER_CLOSE_DELAY_MS);
  };

  useLayoutEffect(() => () => cancelHide(), []);

  useLayoutEffect(() => {
    if (!open) {
      return undefined;
    }

    updatePosition();
    const onReposition = (event) => {
      if (tooltipRef.current && event.target && tooltipRef.current.contains(event.target)) {
        return;
      }
      updatePosition();
    };
    window.addEventListener('scroll', onReposition, true);
    window.addEventListener('resize', onReposition);
    return () => {
      window.removeEventListener('scroll', onReposition, true);
      window.removeEventListener('resize', onReposition);
    };
  }, [open, details]);

  return (
    <div
      ref={chipRef}
      className={`${className ?? MESSAGE_CLASSES.tool} cursor-help`}
      data-tool-details={details || undefined}
      tabIndex={details ? 0 : undefined}
      onMouseEnter={details ? show : undefined}
      onMouseLeave={details ? scheduleHide : undefined}
      onFocus={details ? show : undefined}
      onBlur={details ? scheduleHide : undefined}
    >
      {content}
      {open && details
        ? createPortal(
            <div
              ref={tooltipRef}
              role="tooltip"
              className="fixed z-50 w-max max-w-sm -translate-x-1/2 pt-2"
              style={{ top: coords.top, left: coords.left }}
              onMouseEnter={show}
              onMouseLeave={scheduleHide}
            >
              <pre className="max-h-64 overflow-auto whitespace-pre-wrap rounded-md border border-border bg-popover p-2 font-mono text-xs text-popover-foreground shadow-lg">
                {details}
              </pre>
            </div>,
            document.body,
          )
        : null}
    </div>
  );
}

function createEmptyHistory() {
  return Object.fromEntries(TAB_CONFIG.map((tab) => [tab.id, []]));
}

function createEmptySessions() {
  return Object.fromEntries(TAB_CONFIG.map((tab) => [tab.id, null]));
}

function createEmptySendingState() {
  return Object.fromEntries(TAB_CONFIG.map((tab) => [tab.id, false]));
}

function createEmptyGateState() {
  return Object.fromEntries(
    TAB_CONFIG.filter((tab) => tab.hasGates).map((tab) => [tab.id, { ...GATE_DEFAULTS }]),
  );
}

function ChatPanel() {
  const [activeTab, setActiveTab] = useState('Chat1a');
  const [input, setInput] = useState('');
  const [sendingTabs, setSendingTabs] = useState(createEmptySendingState);
  const [histories, setHistories] = useState(createEmptyHistory);
  const [gateState, setGateState] = useState(createEmptyGateState);
  const [scrollNonce, setScrollNonce] = useState(0);
  const sessionsRef = useRef(createEmptySessions());
  const messagesRef = useRef(null);
  const activeTabRef = useRef(activeTab);
  activeTabRef.current = activeTab;

  const requestScrollToBottom = (tabId) => {
    if (tabId === activeTabRef.current) {
      setScrollNonce((current) => current + 1);
    }
  };

  useLayoutEffect(() => {
    scrollElementToBottom(messagesRef.current);
  }, [activeTab, scrollNonce]);

  const activeConfig = useMemo(
    () => TAB_CONFIG.find((tab) => tab.id === activeTab) ?? TAB_CONFIG[0],
    [activeTab],
  );
  const isActiveTabSending = sendingTabs[activeTab];

  const sendMessage = async () => {
    const message = input.trim();
    if (!message || sendingTabs[activeTab]) return;

    // Capture per-request so concurrent tabs do not share stream state.
    const tabId = activeTab;
    const endpoint = activeConfig.endpoint;
    let assistantText = '';
    let usage = null;

    setInput('');
    setSendingTabs((current) => ({ ...current, [tabId]: true }));

    setHistories((current) => ({
      ...current,
      [tabId]: [...current[tabId], { role: 'user', content: message }],
    }));

    try {
      await streamChatMessage({
        endpoint,
        sessionId: sessionsRef.current[tabId],
        message,
        gates: activeConfig.hasGates ? gateState[tabId] : undefined,
        onEvent: (payload) => {
          if (payload.type === 'session' && payload.sessionId) {
            sessionsRef.current[tabId] = payload.sessionId;
            return;
          }

          if (payload.type === 'token' && payload.text) {
            assistantText += payload.text;
            const snapshot = assistantText;
            setHistories((current) => {
              const tabHistory = [...current[tabId]];
              const last = tabHistory[tabHistory.length - 1];
              if (last?.role === 'assistant' && last.streaming) {
                tabHistory[tabHistory.length - 1] = { role: 'assistant', content: snapshot, streaming: true };
              } else {
                tabHistory.push({ role: 'assistant', content: snapshot, streaming: true });
              }
              return { ...current, [tabId]: tabHistory };
            });
            return;
          }

          if (payload.type === 'tool_start' && payload.toolName) {
            const { toolName, toolArguments } = payload;
            setHistories((current) => ({
              ...current,
              [tabId]: [
                ...current[tabId],
                {
                  role: 'tool',
                  content: `Running ${toolName} …`,
                  toolName,
                  toolArguments,
                  running: true,
                },
              ],
            }));
            return;
          }

          if (payload.type === 'tool_end' && payload.toolName) {
            const { toolName, toolArguments, toolResult } = payload;
            setHistories((current) => {
              const tabHistory = [...current[tabId]];
              const index = findLastIndex(
                tabHistory,
                (entry) => entry.role === 'tool' && entry.running && entry.toolName === toolName,
              );
              if (index === -1) return current;
              const currentEntry = tabHistory[index];
              tabHistory[index] = {
                role: 'tool',
                content: `Ran ${toolName} …`,
                toolName,
                toolArguments: toolArguments || currentEntry.toolArguments,
                toolResult,
              };
              return { ...current, [tabId]: tabHistory };
            });
            return;
          }

          if (payload.type === 'error' && payload.errorMessage) {
            setHistories((current) => ({
              ...current,
              [tabId]: [...current[tabId], { role: 'error', content: payload.errorMessage }],
            }));
            return;
          }

          if (payload.type === 'blocked' && payload.errorMessage) {
            setHistories((current) => ({
              ...current,
              [tabId]: [...current[tabId], { role: 'blocked', content: payload.errorMessage }],
            }));
            return;
          }

          if (payload.type === 'done') {
            usage = payload.usage ?? null;
            requestScrollToBottom(tabId);
          }
        },
      });

      if (assistantText) {
        setHistories((current) => {
          const tabHistory = [...current[tabId]];
          const last = tabHistory[tabHistory.length - 1];
          if (last?.role === 'assistant' && last.streaming) {
            tabHistory[tabHistory.length - 1] = { role: 'assistant', content: assistantText, usage };
          } else {
            tabHistory.push({ role: 'assistant', content: assistantText, usage });
          }
          return { ...current, [tabId]: tabHistory };
        });
      }
    } catch (error) {
      setHistories((current) => ({
        ...current,
        [tabId]: [...current[tabId], { role: 'error', content: error.message || 'Chat failed.' }],
      }));
    } finally {
      setSendingTabs((current) => ({ ...current, [tabId]: false }));
      requestScrollToBottom(tabId);
    }
  };

  const onSubmit = async (event) => {
    event.preventDefault();
    await sendMessage();
  };

  const onKeyDown = (event) => {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing && event.keyCode !== 229) {
      event.preventDefault();
      void sendMessage();
    }
  };

  return (
    <div>
      <h2 className="text-xl font-semibold">Chat Clients</h2>
      <p className="mt-1 text-sm text-muted-foreground">
        Nine standalone chat tabs: Responses API vs Agent Framework (V3 in-process / V4 MCP), plus Chat3 against a hosted Foundry agent (V5), plus Chat4a and Chat4b's multi-agent orchestration (AI Weather Orchestration delegating to Geo and NonAI Weather, in-process for Chat4a and remote MCP for Chat4b), plus Chat5a and Chat5b, the same orchestration with five toggleable guardrail gates.
      </p>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="mt-3 gap-0">
        <TabsList
          variant="line"
          aria-label="Chat client tabs"
          className="flex h-auto w-full flex-wrap justify-start gap-2 bg-transparent p-0"
        >
          {TAB_CONFIG.map((tab) => (
            <TabsTrigger
              key={tab.id}
              value={tab.id}
              aria-label={tab.label}
              title={tab.label}
              className="h-auto flex-none cursor-pointer rounded-md border-2 border-border bg-muted px-3 py-1.5 text-sm font-medium text-foreground shadow-sm after:hidden hover:border-foreground/40 hover:bg-accent hover:text-accent-foreground group-data-[variant=line]/tabs-list:bg-muted group-data-[variant=line]/tabs-list:hover:bg-accent group-data-[variant=line]/tabs-list:data-active:bg-primary group-data-[variant=line]/tabs-list:data-active:text-primary-foreground data-active:border-primary data-active:bg-primary data-active:text-primary-foreground data-active:shadow-none data-active:hover:bg-primary/80 data-active:hover:text-primary-foreground"
            >
              {tab.shortLabel}
            </TabsTrigger>
          ))}
        </TabsList>

        <p className="mt-2 text-sm text-muted-foreground">{activeConfig.description}</p>

        <TabsContent value={activeTab} className="mt-3">
          <section
            className="relative flex flex-col rounded-lg border border-border bg-card p-3"
            aria-label="Chat conversation"
          >
            <div
              ref={messagesRef}
              data-chat-messages
              className="flex min-h-48 max-h-[29rem] flex-col gap-2 overflow-x-auto overflow-y-auto p-1"
            >
              {histories[activeTab].map((entry, index) => (
                entry.role === 'tool' ? (
                  <ToolChip
                    key={`${activeTab}-${index}`}
                    content={entry.content}
                    details={formatToolHoverText(entry)}
                  />
                ) : (
                  <div key={`${activeTab}-${index}`} className={messageClasses(entry)}>
                    {entry.role === 'assistant' && !entry.streaming ? (
                      <SafeGfmMarkdown>{entry.content}</SafeGfmMarkdown>
                    ) : (
                      entry.content
                    )}
                    {entry.role === 'assistant' && !entry.streaming && formatChatUsageChip(entry.usage) ? (
                      <ToolChip
                        content={formatChatUsageChip(entry.usage)}
                        details={formatChatUsageDetails(entry.usage)}
                        className="mt-1.5 w-fit text-xs text-muted-foreground"
                      />
                    ) : null}
                  </div>
                )
              ))}
            </div>

            <form className="mt-3 flex flex-col gap-2 sm:flex-row sm:items-start" onSubmit={onSubmit}>
              <label className="sr-only" htmlFor="chat-input">Message</label>
              <textarea
                id="chat-input"
                className="w-full flex-1 resize-y rounded-md border border-input bg-background px-2.5 py-2 text-foreground focus:border-ring focus:outline-none disabled:bg-muted"
                rows={3}
                value={input}
                placeholder="Ask about weather in a city…"
                onChange={(event) => setInput(event.target.value)}
                onKeyDown={onKeyDown}
                disabled={isActiveTabSending}
              />
              <Button
                className="bg-primary px-4 py-2 text-primary-foreground shadow-sm hover:bg-primary/80"
                type="submit"
                disabled={isActiveTabSending}
              >
                {isActiveTabSending ? 'Sending…' : 'Send'}
              </Button>
            </form>
            {activeConfig.hasGates ? (
              <Chat5GateOptions
                state={gateState[activeTab]}
                onChange={(key, checked) =>
                  setGateState((current) => ({
                    ...current,
                    [activeTab]: { ...current[activeTab], [key]: checked },
                  }))
                }
              />
            ) : null}
          </section>
        </TabsContent>
      </Tabs>
    </div>
  );
}

export default ChatPanel;
