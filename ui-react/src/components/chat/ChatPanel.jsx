import { useLayoutEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Maximize2, Minimize2 } from 'lucide-react';
import SafeGfmMarkdown from '../markdown/SafeGfmMarkdown';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { findLastIndex } from '../../utils/array';
import { formatToolHoverText, TOOL_HOVER_CLOSE_DELAY_MS } from '../../utils/chatToolHover';
import { streamChatMessage } from '../../utils/chatStream';
import { formatChatUsageChip, formatChatUsageDetails } from '../../utils/chatUsage';
import { nativeFullscreenElement, useChatFullscreen } from './useChatFullscreen';

// Tracks the element the tooltip portal should mount into: the current
// native-fullscreen element (which only paints its own subtree) or
// document.body otherwise. Recomputed on fullscreenchange so a tooltip
// that is already open gets reparented immediately, not just on next hover.
function usePortalContainer() {
  const [container, setContainer] = useState(() => nativeFullscreenElement() || document.body);

  // useLayoutEffect (not useEffect) so the reparent + reposition happen
  // before paint, matching the synchronous reparent in the Blazor/MVC
  // fullscreenchange listeners and avoiding a frame where the tooltip
  // renders detached from the (now-hidden) fullscreen subtree.
  useLayoutEffect(() => {
    function onChange() {
      setContainer(nativeFullscreenElement() || document.body);
    }

    document.addEventListener('fullscreenchange', onChange);
    document.addEventListener('webkitfullscreenchange', onChange);
    return () => {
      document.removeEventListener('fullscreenchange', onChange);
      document.removeEventListener('webkitfullscreenchange', onChange);
    };
  }, []);

  return container;
}

const TAB_CONFIG = [
  {
    id: 'Chat1a',
    label: 'Chat1a',
    shortLabel: '1a',
    description: 'Responses API · In-process · Like Foundry Console V3',
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
    description: 'Agent Framework · In-process · Like Foundry Console V3',
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
];

const MESSAGE_CLASSES = {
  user: 'h-max min-h-min shrink-0 self-end max-w-[85%] overflow-visible rounded-2xl bg-primary px-3 py-2 text-primary-foreground whitespace-pre-wrap',
  assistant: 'h-max min-h-min shrink-0 self-start max-w-[85%] overflow-visible rounded-2xl border border-border bg-muted px-3 py-2 text-foreground',
  tool: 'h-max min-h-min shrink-0 self-center text-xs text-muted-foreground',
  error: 'h-max min-h-min shrink-0 w-full rounded-md bg-destructive/15 px-3 py-2 text-destructive',
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

function ToolChip({ content, details, portalContainer, className }) {
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

    // Entering/exiting fullscreen reflows the chat window (and moves the
    // chip), and reparents this tooltip via portalContainer, so both must
    // trigger a fresh measurement — not just scroll/resize.
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
  }, [open, details, portalContainer]);

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
            portalContainer,
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

function ChatPanel() {
  const [activeTab, setActiveTab] = useState('Chat1a');
  const [input, setInput] = useState('');
  const [sendingTabs, setSendingTabs] = useState(createEmptySendingState);
  const [histories, setHistories] = useState(createEmptyHistory);
  const [scrollNonce, setScrollNonce] = useState(0);
  const sessionsRef = useRef(createEmptySessions());
  const messagesRef = useRef(null);
  const windowRef = useRef(null);
  const { isFullscreen, isCssFullscreen, toggle: toggleFullscreen } = useChatFullscreen(windowRef);
  const portalContainer = usePortalContainer();
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
        Five standalone chat tabs: Responses API vs Agent Framework (V3 in-process / V4 MCP), plus Chat3 against a hosted Foundry agent (V5).
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
            ref={windowRef}
            className={`chat-window relative flex flex-col rounded-lg border border-border bg-card p-3 ${
              isCssFullscreen ? 'is-css-fullscreen' : ''
            }`}
            aria-label="Chat conversation"
          >
            <div className="mb-1 flex justify-end">
              <Button
                type="button"
                variant="outline"
                size="icon"
                aria-label={isFullscreen ? 'Exit fullscreen' : 'Enter fullscreen'}
                aria-pressed={isFullscreen}
                onClick={() => {
                  void toggleFullscreen();
                }}
              >
                {isFullscreen ? <Minimize2 className="size-5" /> : <Maximize2 className="size-5" />}
              </Button>
            </div>
            <div
              ref={messagesRef}
              data-chat-messages
              className={`flex min-h-40 flex-col gap-2 overflow-x-auto overflow-y-auto p-1 ${
                isFullscreen ? 'min-h-0 flex-1 max-h-none' : 'max-h-96'
              }`}
            >
              {histories[activeTab].map((entry, index) => (
                entry.role === 'tool' ? (
                  <ToolChip
                    key={`${activeTab}-${index}`}
                    content={entry.content}
                    details={formatToolHoverText(entry)}
                    portalContainer={portalContainer}
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
                        portalContainer={portalContainer}
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
          </section>
        </TabsContent>
      </Tabs>
    </div>
  );
}

export default ChatPanel;
