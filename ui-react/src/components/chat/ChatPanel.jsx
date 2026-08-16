import { useLayoutEffect, useMemo, useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { findLastIndex } from '../../utils/array';
import { streamChatMessage } from '../../utils/chatStream';

const TAB_CONFIG = [
  {
    id: 'Chat1a',
    label: 'Chat1a',
    description: 'In-process · Responses API · Like Foundry Console V3',
    endpoint: '/Chat1a/messages',
  },
  {
    id: 'Chat1b',
    label: 'Chat1b',
    description: 'Remote MCP · Responses API · Like Foundry Console V4',
    endpoint: '/Chat1b/messages',
  },
  {
    id: 'Chat2a',
    label: 'Chat2a',
    description: 'In-process · Agent Framework · Like Foundry Console V3',
    endpoint: '/Chat2a/messages',
  },
  {
    id: 'Chat2b',
    label: 'Chat2b',
    description: 'Remote MCP · Agent Framework · Like Foundry Console V4',
    endpoint: '/Chat2b/messages',
  },
];

const MESSAGE_CLASSES = {
  user: 'self-end max-w-[85%] rounded-2xl bg-primary px-3 py-2 text-primary-foreground whitespace-pre-wrap',
  assistant: 'self-start max-w-[85%] rounded-2xl border border-border bg-muted px-3 py-2 text-foreground whitespace-pre-wrap',
  tool: 'self-center max-w-[85%] rounded-full bg-amber-100 px-3 py-1 text-sm text-amber-900 dark:bg-amber-950 dark:text-amber-200',
  error: 'w-full rounded-md bg-destructive/15 px-3 py-2 text-destructive',
};

function messageClasses(role) {
  return MESSAGE_CLASSES[role] ?? MESSAGE_CLASSES.assistant;
}

function scrollElementToBottom(element) {
  if (!element) {
    return;
  }

  element.scrollTop = element.scrollHeight;
}

function createEmptyHistory() {
  return {
    Chat1a: [],
    Chat1b: [],
    Chat2a: [],
    Chat2b: [],
  };
}

function createEmptySessions() {
  return {
    Chat1a: null,
    Chat1b: null,
    Chat2a: null,
    Chat2b: null,
  };
}

function createEmptySendingState() {
  return {
    Chat1a: false,
    Chat1b: false,
    Chat2a: false,
    Chat2b: false,
  };
}

function ChatPanel() {
  const [activeTab, setActiveTab] = useState('Chat1a');
  const [input, setInput] = useState('');
  const [sendingTabs, setSendingTabs] = useState(createEmptySendingState);
  const [histories, setHistories] = useState(createEmptyHistory);
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
            const { toolName } = payload;
            setHistories((current) => ({
              ...current,
              [tabId]: [
                ...current[tabId],
                { role: 'tool', content: `Running ${toolName}…`, toolName, running: true },
              ],
            }));
            return;
          }

          if (payload.type === 'tool_end' && payload.toolName) {
            const { toolName } = payload;
            setHistories((current) => {
              const tabHistory = [...current[tabId]];
              const index = findLastIndex(
                tabHistory,
                (entry) => entry.role === 'tool' && entry.running && entry.toolName === toolName,
              );
              if (index === -1) return current;
              tabHistory[index] = { role: 'tool', content: `Ran ${toolName}`, toolName };
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
            requestScrollToBottom(tabId);
          }
        },
      });

      if (assistantText) {
        setHistories((current) => {
          const tabHistory = [...current[tabId]];
          const last = tabHistory[tabHistory.length - 1];
          if (last?.role === 'assistant' && last.streaming) {
            tabHistory[tabHistory.length - 1] = { role: 'assistant', content: assistantText };
          } else {
            tabHistory.push({ role: 'assistant', content: assistantText });
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
        Four standalone chat tabs: Responses API vs Agent Framework, each with in-process (V3) or MCP (V4) tools.
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
              className="h-auto flex-none cursor-pointer rounded-md border-2 border-border bg-muted px-3 py-1.5 text-sm font-medium text-foreground shadow-sm after:hidden hover:border-foreground/40 hover:bg-accent hover:text-accent-foreground group-data-[variant=line]/tabs-list:bg-muted group-data-[variant=line]/tabs-list:hover:bg-accent group-data-[variant=line]/tabs-list:data-active:bg-primary group-data-[variant=line]/tabs-list:data-active:text-primary-foreground data-active:border-primary data-active:bg-primary data-active:text-primary-foreground data-active:shadow-none data-active:hover:bg-primary/80 data-active:hover:text-primary-foreground"
            >
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>

        <p className="mt-2 text-sm text-muted-foreground">{activeConfig.description}</p>

        <TabsContent value={activeTab} className="mt-3">
          <section className="rounded-lg border border-border bg-card p-3" aria-label="Chat conversation">
            <div
              ref={messagesRef}
              data-chat-messages
              className="flex max-h-96 min-h-40 flex-col gap-2 overflow-y-auto p-1"
            >
              {histories[activeTab].map((entry, index) => (
                <div key={`${activeTab}-${index}`} className={messageClasses(entry.role)}>
                  {entry.content}
                </div>
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
