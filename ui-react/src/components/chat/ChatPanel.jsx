import { useMemo, useRef, useState } from 'react';
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

function ChatPanel() {
  const [activeTab, setActiveTab] = useState('Chat1a');
  const [input, setInput] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [histories, setHistories] = useState(createEmptyHistory);
  const sessionsRef = useRef(createEmptySessions());
  const streamingAssistantRef = useRef('');

  const activeConfig = useMemo(
    () => TAB_CONFIG.find((tab) => tab.id === activeTab) ?? TAB_CONFIG[0],
    [activeTab],
  );

  const onSubmit = async (event) => {
    event.preventDefault();
    const message = input.trim();
    if (!message || isSending) return;

    setInput('');
    setIsSending(true);
    streamingAssistantRef.current = '';

    setHistories((current) => ({
      ...current,
      [activeTab]: [...current[activeTab], { role: 'user', content: message }],
    }));

    try {
      await streamChatMessage({
        endpoint: activeConfig.endpoint,
        sessionId: sessionsRef.current[activeTab],
        message,
        onEvent: (payload) => {
          if (payload.type === 'session' && payload.sessionId) {
            sessionsRef.current[activeTab] = payload.sessionId;
            return;
          }

          if (payload.type === 'token' && payload.text) {
            streamingAssistantRef.current += payload.text;
            const snapshot = streamingAssistantRef.current;
            setHistories((current) => {
              const tabHistory = [...current[activeTab]];
              const last = tabHistory[tabHistory.length - 1];
              if (last?.role === 'assistant' && last.streaming) {
                tabHistory[tabHistory.length - 1] = { role: 'assistant', content: snapshot, streaming: true };
              } else {
                tabHistory.push({ role: 'assistant', content: snapshot, streaming: true });
              }
              return { ...current, [activeTab]: tabHistory };
            });
            return;
          }

          if (payload.type === 'tool_start' && payload.toolName) {
            setHistories((current) => ({
              ...current,
              [activeTab]: [...current[activeTab], { role: 'tool', content: `Running ${payload.toolName}…` }],
            }));
            return;
          }

          if (payload.type === 'error' && payload.errorMessage) {
            setHistories((current) => ({
              ...current,
              [activeTab]: [...current[activeTab], { role: 'error', content: payload.errorMessage }],
            }));
          }
        },
      });

      const finalText = streamingAssistantRef.current;
      if (finalText) {
        setHistories((current) => {
          const tabHistory = [...current[activeTab]];
          const last = tabHistory[tabHistory.length - 1];
          if (last?.role === 'assistant' && last.streaming) {
            tabHistory[tabHistory.length - 1] = { role: 'assistant', content: finalText };
          } else {
            tabHistory.push({ role: 'assistant', content: finalText });
          }
          return { ...current, [activeTab]: tabHistory };
        });
      }
    } catch (error) {
      setHistories((current) => ({
        ...current,
        [activeTab]: [...current[activeTab], { role: 'error', content: error.message || 'Chat failed.' }],
      }));
    } finally {
      streamingAssistantRef.current = '';
      setIsSending(false);
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
            disabled={isSending}
          />
          <button className="chat-send" type="submit" disabled={isSending}>
            {isSending ? 'Sending…' : 'Send'}
          </button>
        </form>
      </section>
    </div>
  );
}

export default ChatPanel;
