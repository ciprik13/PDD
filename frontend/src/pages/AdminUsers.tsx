import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '@/services/api';
import type { User } from '@/types';
import styles from './shared.module.css';

export function AdminUsersPage() {
  const { t } = useTranslation();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');

  async function refresh() {
    setLoading(true);
    try {
      setUsers(await api.getUsers());
    } catch {
      setError(t('common.loadError'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void refresh(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    setOk('');
    setSubmitting(true);
    try {
      const u = await api.createAgricultureUser({ email: email.trim(), password });
      setOk(t('users.created', { email: u.email }));
      setEmail('');
      setPassword('');
      await refresh();
    } catch {
      setError(t('users.createError'));
    } finally {
      setSubmitting(false);
    }
  }

  async function toggleAgriculture(u: User) {
    const assigned = !u.roles.includes('agriculture');
    setBusyId(u.id);
    try {
      await api.setAgricultureRole(u.id, assigned);
      await refresh();
    } catch {
      setError(t('common.loadError'));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('users.title')}</h1>
          <p className={styles.pageSub}>{t('users.subtitle')}</p>
        </div>
      </div>

      {/* Create agriculture user */}
      <div className={styles.card} style={{ marginBottom: 14 }}>
        <div className={styles.cardTitle}>{t('users.createTitle')}</div>
        <div className={styles.cardSub}>{t('users.createHint')}</div>
        <form onSubmit={handleCreate}>
          <div className={styles.field}>
            <label className={styles.fieldLabel} htmlFor="uemail">{t('users.email')}</label>
            <input id="uemail" className={styles.input} type="email" value={email}
              onChange={(e) => setEmail(e.target.value)} placeholder="grower@farm.com" required />
          </div>
          <div className={styles.field}>
            <label className={styles.fieldLabel} htmlFor="upass">{t('users.password')}</label>
            <input id="upass" className={styles.input} type="password" value={password}
              onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" required />
          </div>
          {error && <p className={styles.empty} style={{ color: 'var(--red-txt, #b91c1c)' }}>{error}</p>}
          {ok && <p className={styles.empty} style={{ color: 'var(--forest)' }}>{ok}</p>}
          <button type="submit" className={styles.btn} disabled={submitting}>
            {submitting ? t('common.loading') : t('users.createBtn')}
          </button>
        </form>
      </div>

      {/* User list */}
      <div className={styles.card}>
        <div className={styles.cardTitle}>{t('users.listTitle')}</div>
        {loading && <p className={styles.empty}>{t('common.loading')}</p>}
        {!loading && users.length === 0 && <p className={styles.empty}>{t('users.empty')}</p>}
        {users.map((u) => {
          const isAgri = u.roles.includes('agriculture');
          const isAdmin = u.roles.includes('admin');
          return (
            <div key={u.id} className={styles.listItem}>
              <div className={styles.listDot} style={{ background: isAgri ? 'var(--forest-3)' : '#9ca3af' }} />
              <div style={{ flex: 1 }}>
                <div className={styles.listTitle}>{u.email}</div>
                <div className={styles.listDesc}>
                  {u.roles.length > 0 ? u.roles.join(' · ') : t('users.noRoles')}
                </div>
              </div>
              {!isAdmin && (
                <button
                  className={isAgri ? styles.btnGhost : styles.btn}
                  disabled={busyId === u.id}
                  onClick={() => toggleAgriculture(u)}
                >
                  {busyId === u.id ? t('common.loading') : isAgri ? t('users.revokeAgri') : t('users.grantAgri')}
                </button>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
