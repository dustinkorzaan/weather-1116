import './App.css';
import { useEffect, useRef, useState } from 'react';

function App() {
  const [helloMessage, setHelloMessage] = useState('Loading hello message...');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const avatarMenuRef = useRef(null);

  useEffect(() => {
    let isMounted = true;

    fetch('/Home/Hello')
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Request failed: ${response.status}`);
        }

        return response.json();
      })
      .then((data) => {
        if (isMounted) {
          setHelloMessage(data.requestResponse ?? 'No hello response returned.');
        }
      })
      .catch(() => {
        if (isMounted) {
          setHelloMessage('Unable to load hello message from API.');
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  useEffect(() => {
    if (!isMenuOpen) {
      return undefined;
    }

    const handleClickOutside = (event) => {
      if (avatarMenuRef.current && !avatarMenuRef.current.contains(event.target)) {
        setIsMenuOpen(false);
      }
    };

    const handleKeyDown = (event) => {
      if (event.key === 'Escape') {
        setIsMenuOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isMenuOpen]);

  const handleAvatarClick = () => {
    setIsMenuOpen((open) => !open);
  };

  const handleAboutClick = () => {
    setIsMenuOpen(false);
    setIsAboutOpen(true);
  };

  const closeAboutModal = () => {
    setIsAboutOpen(false);
  };

  return (
    <div className="app">
      <header className="top-bar">
        <div className="site-brand">
          <img src="/logo.svg" alt="Weather logo" className="site-logo" />
          <h1 className="title">Weather React</h1>
        </div>

        <div className="avatar-menu" ref={avatarMenuRef}>
          <button
            type="button"
            className="avatar-button"
            aria-haspopup="true"
            aria-expanded={isMenuOpen}
            aria-label="Open user menu"
            onClick={handleAvatarClick}
          >
            <svg viewBox="0 0 24 24" className="avatar-icon" aria-hidden="true" focusable="false">
              <circle cx="12" cy="8" r="4" fill="currentColor" />
              <path
                d="M4 20c0-4.418 3.582-8 8-8s8 3.582 8 8"
                fill="currentColor"
              />
            </svg>
          </button>

          {isMenuOpen && (
            <ul className="avatar-dropdown" role="menu">
              <li role="none">
                <button type="button" role="menuitem" className="avatar-dropdown-item" onClick={handleAboutClick}>
                  About
                </button>
              </li>
            </ul>
          )}
        </div>
      </header>

      <main className="home-content">
        <p className="hello-message">{helloMessage}</p>
      </main>

      {isAboutOpen && (
        <div className="modal-backdrop" role="presentation" onClick={closeAboutModal}>
          <div
            className="modal-dialog"
            role="dialog"
            aria-modal="true"
            aria-labelledby="about-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="modal-header">
              <h2 id="about-modal-title" className="modal-title">About</h2>
              <button
                type="button"
                className="modal-close"
                aria-label="Close"
                onClick={closeAboutModal}
              >
                &times;
              </button>
            </div>
            {/* Placeholder body - intentionally blank. About content to be added in a future story. */}
            <div className="modal-body"></div>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
