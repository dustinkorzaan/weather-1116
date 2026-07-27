import './App.css';
import { useEffect, useRef, useState } from 'react';
import { Link, Route, Routes } from 'react-router-dom';
import { siteLinks } from './config/siteLinks';
import HomePage from './pages/HomePage';
import {
  useLazyGetAboutQuery,
} from './services/weatherApi';

/** Formats a build timestamp like "7/12/2026 10:22:14 PM UTC" (matches MVC and Blazor). */
function formatBuildStart(isoDate) {
  const date = new Date(isoDate);
  if (Number.isNaN(date.getTime())) {
    return String(isoDate);
  }

  const month = date.getUTCMonth() + 1;
  const day = date.getUTCDate();
  const year = date.getUTCFullYear();
  const minutes = String(date.getUTCMinutes()).padStart(2, '0');
  const seconds = String(date.getUTCSeconds()).padStart(2, '0');
  const period = date.getUTCHours() >= 12 ? 'PM' : 'AM';
  const hours = date.getUTCHours() % 12 || 12;

  return `${month}/${day}/${year} ${hours}:${minutes}:${seconds} ${period} UTC`;
}

function AboutTreeNode({ node }) {
  if (!node) {
    return null;
  }

  const hasChildren = Array.isArray(node.children) && node.children.length > 0;
  const metadata = [];
  if (Number.isFinite(node.buildNumber)) {
    metadata.push({ text: `Build #${node.buildNumber}`, value: node.buildNumber });
  }
  if (node.buildStart) {
    metadata.push({ text: `Started ${formatBuildStart(node.buildStart)}`, value: formatBuildStart(node.buildStart) });
  }
  if (node.buildBranchName) {
    metadata.push({ text: `Branch ${node.buildBranchName}`, value: node.buildBranchName, isBranch: true });
  }

  return (
    <li className="about-tree-item">
      <div className="about-tree-row">
        <span className="about-tree-name">{node.name ?? 'Unnamed node'}</span>
        <span className={`about-tree-health ${node.isHealthy ? 'healthy' : 'unhealthy'}`}>
          {node.isHealthy ? 'Healthy' : 'Unhealthy'}
        </span>
      </div>
      {node.message && <div className="about-tree-message">{node.message}</div>}
      {metadata.length > 0 && (
        <div className="about-tree-meta">
          {metadata.map((item, index) => (
            <span key={`${item.text}-${index}`}>
              {index > 0 && ' | '}
              <span className={item.isBranch && item.value !== 'main' ? 'about-tree-branch-non-main' : undefined}>
                {item.text}
              </span>
            </span>
          ))}
        </div>
      )}

      {hasChildren && (
        <ul className="about-tree-list">
          {node.children.map((child, index) => (
            <AboutTreeNode key={`${child.name ?? 'node'}-${index}`} node={child} />
          ))}
        </ul>
      )}
    </li>
  );
}

function SiteLinksFooter() {
  return (
    <div className="site-links-footer">
      {siteLinks.map((link) => (
        <a key={link.label} href={link.href} target="_blank" rel="noopener noreferrer">
          {link.label}
        </a>
      ))}
    </div>
  );
}

function App() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const avatarMenuRef = useRef(null);
  const [loadAbout, aboutQuery] = useLazyGetAboutQuery();

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
    loadAbout();
  };

  const closeAboutModal = () => {
    setIsAboutOpen(false);
  };

  return (
    <div className="app">
      <header className="top-bar">
        <div className="top-bar-inner">
          <Link className="site-brand" to="/">
            <img src="/logo.svg" alt="Weather logo" className="site-logo" />
            <h1 className="title">Weather React</h1>
          </Link>

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
                {siteLinks.map((link) => (
                  <li key={link.label} role="none">
                    <a
                      className="avatar-dropdown-item"
                      role="menuitem"
                      href={link.href}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      {link.label}
                    </a>
                  </li>
                ))}
                <li role="separator" className="avatar-dropdown-divider" />
                <li role="none">
                  <button type="button" role="menuitem" className="avatar-dropdown-item" onClick={handleAboutClick}>
                    About
                  </button>
                </li>
              </ul>
            )}
          </div>
        </div>
      </header>

      <Routes>
        <Route path="/" element={<HomePage />} />
      </Routes>

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
            <div className="modal-body">
              {aboutQuery.isFetching && (
                <p className="about-status loading">
                  <span className="about-spinner" aria-hidden="true"></span>
                  <span>Loading About information...</span>
                </p>
              )}
              {!aboutQuery.isFetching && aboutQuery.isError && (
                <p className="about-status error">Unable to load About information.</p>
              )}
              {!aboutQuery.isFetching && !aboutQuery.isError && aboutQuery.data && (
                <ul className="about-tree-list root">
                  <AboutTreeNode node={aboutQuery.data} />
                </ul>
              )}
              <SiteLinksFooter />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
