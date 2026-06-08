import { useTranslation } from 'react-i18next';
import { useStandPosition } from '@/hooks/useApi';
import styles from './shared.module.css';

export function StandPositionPage() {
  const { t } = useTranslation();
  const { data: p, loading } = useStandPosition();

  const hasData = p && (p.row != null || p.positionMeters != null);

  const cells: { label: string; value: string; unit: string }[] = p
    ? [
        {
          label: t('standPosition.row'),
          value: p.row != null ? `${p.row}${p.totalRows != null ? `/${p.totalRows}` : ''}` : '—',
          unit: t('standPosition.rowUnit'),
        },
        {
          label: t('standPosition.pos'),
          value: p.positionMeters != null ? p.positionMeters.toFixed(1) : '—',
          unit: 'm',
        },
      ]
    : [];

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('standPosition.title')}</h1>
          <p className={styles.pageSub}>{t('standPosition.subtitle')}</p>
        </div>
      </div>

      {loading && <div className={styles.card}><p className={styles.empty}>{t('common.loading')}</p></div>}
      {!loading && !hasData && (
        <div className={styles.card}><p className={styles.empty}>{t('standPosition.empty')}</p></div>
      )}
      {!loading && hasData && (
        <div className={styles.grid3}>
          {cells.map((c) => (
            <div key={c.label} className={styles.card}>
              <div className={styles.statLabel}>{c.label}</div>
              <div className={styles.statValue}>{c.value}</div>
              <div className={styles.statUnit}>{c.unit}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
