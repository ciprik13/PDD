import { type ReactNode } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  LayoutDashboard, Camera, Star, Plus, Home, LogOut,
  Leaf, KeyRound, Users,
} from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import styles from './Sidebar.module.css';

interface NavItem {
  to: string;
  icon: ReactNode;
  labelKey: string;
  badge?: number;
  live?: boolean;
  adminOnly?: boolean;
}

const LANGUAGES = [
  { code: 'en', label: 'EN' },
  { code: 'ro', label: 'RO' },
  { code: 'ru', label: 'RU' },
] as const;

export function Sidebar() {
  const { t, i18n } = useTranslation();
  const { logout, isAdmin } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const navGroups: { labelKey: string; items: NavItem[] }[] = [
    {
      labelKey: 'sidebar.nav.groups.monitor',
      items: [
        { to: '/', icon: <LayoutDashboard size={15} />, labelKey: 'sidebar.nav.items.home' },
        { to: '/camera', icon: <Camera size={15} />, labelKey: 'sidebar.nav.items.liveCamera', live: true },
      ],
    },
    {
      labelKey: 'sidebar.nav.groups.detection',
      items: [
        { to: '/plants', icon: <Leaf size={15} />, labelKey: 'sidebar.nav.items.plants' },
        { to: '/passport', icon: <Star size={15} />, labelKey: 'sidebar.nav.items.plantPassport' },
      ],
    },
    {
      labelKey: 'sidebar.nav.groups.treatment',
      items: [
        { to: '/treatments', icon: <Plus size={15} />, labelKey: 'sidebar.nav.items.recommendations' },
        { to: '/history', icon: <Home size={15} />, labelKey: 'sidebar.nav.items.treatmentHistory' },
      ],
    },
    {
      labelKey: 'sidebar.nav.groups.system',
      items: [
        { to: '/admin/users', icon: <Users size={15} />, labelKey: 'sidebar.nav.items.users', adminOnly: true },
        { to: '/admin/api-keys', icon: <KeyRound size={15} />, labelKey: 'sidebar.nav.items.apiKeys', adminOnly: true },
      ],
    },
  ];

  const visibleGroups = navGroups
    .map((group) => ({ ...group, items: group.items.filter((i) => !i.adminOnly || isAdmin) }))
    .filter((group) => group.items.length > 0);

  const changeLanguage = (code: string) => {
    i18n.changeLanguage(code);
    localStorage.setItem('agricure-lang', code);
  };

  return (
    <aside className={styles.sidebar}>
      {/* Logo */}
      <div className={styles.logoBox}>
        <img src="/agricure.png" alt="AgriCure" className={styles.logoImg} />
        <div className={styles.logoName}>AgriCure</div>
        <button className={styles.logoutIconBtn} onClick={handleLogout} title={t('sidebar.logout')}>
          <LogOut size={14} />
        </button>
      </div>

      {/* Navigation */}
      <nav className={styles.nav}>
        {visibleGroups.map((group) => (
          <div key={group.labelKey} className={styles.navGroup}>
            <div className={styles.groupLabel}>{t(group.labelKey)}</div>
            {group.items.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  [styles.navItem, isActive ? styles.active : ''].join(' ')
                }
              >
                <span className={styles.navIcon}>{item.icon}</span>
                {t(item.labelKey)}
                {item.badge !== undefined && (
                  <span className={styles.badge}>{item.badge}</span>
                )}
                {item.live && <span className={styles.liveDot} />}
              </NavLink>
            ))}
          </div>
        ))}
      </nav>

      {/* Language switcher */}
      <div className={styles.langSwitcher}>
        {LANGUAGES.map((lang) => (
          <button
            key={lang.code}
            className={[styles.langBtn, i18n.language === lang.code ? styles.langBtnActive : ''].join(' ')}
            onClick={() => changeLanguage(lang.code)}
          >
            {lang.label}
          </button>
        ))}
      </div>

    </aside>
  );
}
