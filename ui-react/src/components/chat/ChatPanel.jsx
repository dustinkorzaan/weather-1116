import { useMemo, useRef, useState } from 'react';
import { findLastIndex } from '../../utils/array';
import { streamChatMessage } from '../../utils/chatStream';

const TAB_CONFIG = [
  {
    id: 'Chat1a',
    label: 'Chat1a',
    description: 'Responses API · in-process tools (V3 loop)',
    endpoint: '/Chat1a/messages',
  },
  {
    id: 'Chat1b',
    label: 'Chat1b',
    description: 'Responses API · remote MCP tools (V4)',
    endpoint: '/Chat1b/messages',
  },
  {
    id: 'Chat2a',
    label: 'Chat2a',
    description: 'Agent Framework · in-process tools',
    endpoint: '/Chat2a/messages',
  },
  {
    id: 'Chat2b',
    label: 'Chat2b',
    description: 'Agent Framework · remote MCP tools',
    endpoint: '/Chat2b/messages',
  },
];

const MESSAGE_CLASSES = {
  user: 'self-end max-w-[85%] rounded-2xl bg-blue-600 px-3 py-2 text-white whitespace-pre-wrap',
  assistant: 'self-start max-w-[85%] rounded-2xl border bg-gray-100 px-3 py-2 text-gray-900 whitespace-pre-wrap',
  tool: 'self-center max-w-[85%] rounded-full bg-amber-100 px-3 py-1 text-sm text-amber-900',
  error: 'w-full rounded-md bg-red-100 px-3 py-2 text-red-800',
};

function messageClasses(role) {
  return MESSAGE_CLASSES[role] ?? MESSAGE_CLASSES.assistant;
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
  const sessionsRef = useRef(createEmptySessions());

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
    <div className="mt-6">
      <h2 className="text-xl font-semibold">Chat Clients</h2>
      <p className="mt-1 text-sm text-gray-600">
        Four standalone chat tabs: Responses API vs Agent Framework, each with in-process (V3) or MCP (V4) tools.
      </p>

      <div className="mt-3 flex flex-wrap gap-2" role="tablist" aria-label="Chat client tabs">
        {TAB_CONFIG.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={
              activeTab === tab.id
                ? 'rounded-md border border-blue-600 bg-blue-600 px-3 py-1.5 text-sm font-medium text-white'
                : 'rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-100'
            }
            role="tab"
            aria-selected={activeTab === tab.id}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <p className="mt-2 text-sm text-gray-600">{activeConfig.description}</p>

      <section className="mt-3 rounded-lg border border-gray-300 bg-white p-3" aria-label="Chat conversation">
        <div className="flex max-h-96 min-h-40 flex-col gap-2 overflow-y-auto p-1">
          {histories[activeTab].map((entry, index) => (
            <div key={`${activeTab}-${index}`} className={messageClasses(entry.role)}>
              {entry.content}
            </div>
          ))}
        </div>

        <form className="mt-3 flex flex-col gap-2 sm:flex-row sm:items-end" onSubmit={onSubmit}>
          <label className="sr-only" htmlFor="chat-input">Message</label>
          <textarea
            id="chat-input"
            className="w-full flex-1 resize-y rounded-md border border-gray-300 px-2.5 py-2 focus:border-blue-600 focus:outline-none disabled:bg-gray-100"
            rows={3}
            value={input}
            placeholder="Ask about weather in a city…"
            onChange={(event) => setInput(event.target.value)}
            onKeyDown={onKeyDown}
            disabled={isActiveTabSending}
          />
          <button
            className="rounded-md bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-700 disabled:opacity-60"
            type="submit"
            disabled={isActiveTabSending}
          >
            {isActiveTabSending ? 'Sending…' : 'Send'}
          </button>
        </form>
      </section>
    </div>
  );
}

export default ChatPanel;
