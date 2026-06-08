import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { formatDistanceToNow } from 'date-fns';
import { api } from '@/services/api';
import type { ApiKey, ApiKeyCreated } from '@/types';
import styles from './shared.module.css';

export function AdminApiKeysPage() {
  const { t } = useTranslation();
  const [keys, setKeys] = useState<ApiKey[]>([]);
  const [loading, setLoading] = useState(true);
  const [ownerUserId, setOwnerUserId] = useState('');
  const [name, setName] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [created, setCreated] = useState<ApiKeyCreated | null>(null);

  async function refresh() {
    setLoading(true);
    try {
      setKeys(await api.getApiKeys(true));
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
    setCreated(null);
    setSubmitting(true);
    try {
      const result = await api.createApiKey({ ownerUserId: ownerUserId.trim(), name: name.trim() });
      setCreated(result);
      setOwnerUserId('');
      setName('');
      await refresh();
    } catch {
      setError(t('apiKeys.createError'));
    } finally {
      setSubmitting(false);
    }
  }

  async function handleRevoke(id: string) {
    await api.revokeApiKey(id).catch(() => {});
    await refresh();
  }

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('apiKeys.title')}</h1>
          <p className={styles.pageSub}>{t('apiKeys.subtitle')}</p>
        </div>
      </div>

      {/* Create form */}
      <div className={styles.card} style={{ marginBottom: 14 }}>
        <div className={styles.cardTitle}>{t('apiKeys.createTitle')}</div>
        <div className={styles.cardSub}>{t('apiKeys.createHint')}</div>
        <form onSubmit={handleCreate}>
          <div className={styles.field}>
            <label className={styles.fieldLabel} htmlFor="owner">{t('apiKeys.ownerLabel')}</label>
            <input
              id="owner"
              className={styles.input}
              value={ownerUserId}
              onChange={(e) => setOwnerUserId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
              required
            />
          </div>
          <div className={styles.field}>
            <label className={styles.fieldLabel} htmlFor="keyname">{t('apiKeys.nameLabel')}</label>
            <input
              id="keyname"
              className={styles.input}
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="north-field-camera-1"
              required
            />
          </div>
          {error && <p className={styles.empty} style={{ color: 'var(--red-txt, #b91c1c)' }}>{error}</p>}
          <button type="submit" className={styles.btn} disabled={submitting}>
            {submitting ? t('common.loading') : t('apiKeys.createBtn')}
          </button>
        </form>

        {created && (
          <div className={styles.card} style={{ marginTop: 14, background: 'var(--mist)' }}>
            <div className={styles.cardTitle}>{t('apiKeys.createdTitle')}</div>
            <div className={styles.cardSub}>{t('apiKeys.createdHint')}</div>
            <code className={styles.mono} style={{ wordBreak: 'break-all' }}>{created.plaintextKey}</code>
            <div style={{ marginTop: 10 }}>
              <button
                className={styles.btn}
                onClick={() => navigator.clipboard?.writeText(created.plaintextKey)}
              >
                {t('apiKeys.copy')}
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Key list */}
      <div className={styles.card}>
        <div className={styles.cardTitle}>{t('apiKeys.listTitle')}</div>
        {loading && <p className={styles.empty}>{t('common.loading')}</p>}
        {!loading && keys.length === 0 && <p className={styles.empty}>{t('apiKeys.empty')}</p>}
        {keys.map((k) => (
          <div key={k.id} className={styles.listItem}>
            <div className={styles.listDot} style={{ background: k.isActive ? 'var(--forest-3)' : '#9ca3af' }} />
            <div style={{ flex: 1 }}>
              <div className={styles.listTitle}>
                {k.name} <span className={styles.mono} style={{ color: 'var(--txt-3)' }}>····{k.tokenLast4}</span>
              </div>
              <div className={styles.listDesc}>
                {k.scope} · {k.isActive ? t('apiKeys.active') : t('apiKeys.revoked')}
                {k.lastUsedAt ? ` · ${t('apiKeys.lastUsed', { when: formatDistanceToNow(new Date(k.lastUsedAt), { addSuffix: true }) })}` : ` · ${t('apiKeys.neverUsed')}`}
              </div>
            </div>
            {k.isActive && (
              <button className={styles.btnGhost} onClick={() => handleRevoke(k.id)}>
                {t('apiKeys.revoke')}
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
