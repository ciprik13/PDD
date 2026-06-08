import { useTranslation } from 'react-i18next';
import { formatDistanceToNow } from 'date-fns';
import { ro, ru, enUS } from 'date-fns/locale';
import { useAppliedTreatments } from '@/hooks/useApi';
import type { AppliedTreatment } from '@/types';
import styles from './shared.module.css';

export function TreatmentHistoryPage() {
  const { t, i18n } = useTranslation();
  const { data: history, loading } = useAppliedTreatments();
  const locale = i18n.language === 'ro' ? ro : i18n.language === 'ru' ? ru : enUS;

  function dotColor(h: AppliedTreatment) {
    return h.type === 'biological' ? 'var(--forest-3)' : '#f59e0b';
  }

  function desc(h: AppliedTreatment) {
    const base = t('history.appliedPlant', {
      plant: h.plantId,
      row: h.row,
      type: t(`history.type.${h.type}`),
    });
    const phi = h.phiDays > 0 ? ` · ${t('history.phiDays', { days: h.phiDays })}` : '';
    const note = h.notes ? ` · ${h.notes}` : '';
    return `${base}${phi}${note}`;
  }

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('treatmentHistory.title')}</h1>
          <p className={styles.pageSub}>{t('treatmentHistory.subtitle')}</p>
        </div>
      </div>

      <div className={styles.card}>
        {loading && <p className={styles.empty}>{t('common.loading')}</p>}
        {!loading && (history?.length ?? 0) === 0 && (
          <p className={styles.empty}>{t('treatmentHistory.empty')}</p>
        )}
        {(history ?? []).map((h) => (
          <div key={h.id} className={styles.listItem}>
            <div className={styles.listDot} style={{ background: dotColor(h) }} />
            <div>
              <div className={styles.listTitle}>{h.treatmentName} — {h.dosage}</div>
              <div className={styles.listDesc}>{desc(h)}</div>
              <div className={styles.listTime}>
                {formatDistanceToNow(new Date(h.appliedAt), { addSuffix: true, locale })}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
