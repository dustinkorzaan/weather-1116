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

  const onSubmit = async (event) => {
    event.preventDefault();
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

  return (
    <div className="chat-page">
      <h2 className="chat-page-title">Chat Clients</h2>
      <p className="chat-page-lead">
        Four standalone chat tabs: Responses API vs Agent Framework, each with in-process (V3) or MCP (V4) tools.
      </p>

      <div className="chat-tabs" role="tablist" aria-label="Chat client tabs">
        {TAB_CONFIG.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={`chat-tab${activeTab === tab.id ? ' active' : ''}`}
            role="tab"
            aria-selected={activeTab === tab.id}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <p className="chat-tab-description">{activeConfig.description}</p>

      <section className="chat-panel" aria-label="Chat conversation">
        <div className="chat-messages">
          {histories[activeTab].map((entry, index) => (
            <div key={`${activeTab}-${index}`} className={`chat-message chat-message-${entry.role}`}>
              {entry.content}
            </div>
          ))}
        </div>

        <form className="chat-form" onSubmit={onSubmit}>
          <label className="visually-hidden" htmlFor="chat-input">Message</label>
          <textarea
            id="chat-input"
            className="chat-input"
            rows={3}
            value={input}
            placeholder="Ask about weather in a city…"
            onChange={(event) => setInput(event.target.value)}
            disabled={isActiveTabSending}
          />
          <button className="chat-send" type="submit" disabled={isActiveTabSending}>
            {isActiveTabSending ? 'Sending…' : 'Send'}
          </button>
        </form>
      </section>
    </div>
  );
}

export default ChatPanel;
