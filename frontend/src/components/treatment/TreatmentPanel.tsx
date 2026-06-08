import { useTranslation } from 'react-i18next';
import { Card, CardHeader, Chip } from '@/components/shared/UI';
import type { Treatment } from '@/types';
import styles from './TreatmentPanel.module.css';

interface TreatmentPanelProps {
  treatments: Treatment[];
  diseaseName?: string;
  /** When provided, each treatment card shows an "Apply" button. */
  onApply?: (treatment: Treatment) => void;
  /** Disables the Apply buttons (e.g. while a request is in flight or no plant is selected). */
  applyDisabled?: boolean;
  applyingId?: string | null;
}

export function TreatmentPanel({ treatments, diseaseName, onApply, applyDisabled, applyingId }: TreatmentPanelProps) {
  const { t } = useTranslation();
  return (
    <Card>
      <CardHeader
        title={`${t('treatment.title')}${diseaseName ? ` — ${diseaseName}` : ''}`}
        subtitle={t('treatment.subtitle')}
        right={<Chip label={t('treatment.actNow')} variant="amber" />}
      />
      <div className={styles.list}>
        {treatments.map((tr) => (
          <TreatmentCard
            key={tr.id}
            treatment={tr}
            onApply={onApply}
            applyDisabled={applyDisabled}
            applying={applyingId === tr.id}
          />
        ))}
      </div>
    </Card>
  );
}

function TreatmentCard({
  treatment: tr,
  onApply,
  applyDisabled,
  applying,
}: {
  treatment: Treatment;
  onApply?: (treatment: Treatment) => void;
  applyDisabled?: boolean;
  applying?: boolean;
}) {
  const { t } = useTranslation();
  const isBio = tr.type === 'biological';

  const computedTags = [
    t(`treatment.type.${tr.type}`),
    t('treatment.phi', { days: tr.phiDays }),
    t(`treatment.cost.${tr.costLevel}`),
  ];

  return (
    <div className={[styles.card, isBio ? styles.cardBio : styles.cardChem].join(' ')}>
      <div className={styles.head}>
        <span className={styles.name}>{tr.name}</span>
        <Chip label={isBio ? t('treatment.bioFirst') : t('treatment.chemical')} variant={isBio ? 'green' : 'red'} />
      </div>
      <p className={styles.desc}>
        {tr.descriptionKey ? t(tr.descriptionKey) : tr.description}
      </p>
      <div className={styles.tags}>
        {computedTags.map((tag) => (
          <span
            key={tag}
            className={styles.tag}
            style={
              isBio
                ? { background: 'var(--mist)', color: 'var(--forest)', borderColor: 'var(--br-2)' }
                : { background: 'var(--red-bg)', color: 'var(--red-txt)', borderColor: 'var(--red-br)' }
            }
          >
            {tag}
          </span>
        ))}
      </div>
      {onApply && (
        <button
          type="button"
          disabled={applyDisabled || applying}
          onClick={() => onApply(tr)}
          style={{
            marginTop: 10,
            fontSize: 12,
            fontWeight: 700,
            color: '#fff',
            background: applyDisabled || applying ? '#9ca3af' : 'var(--forest)',
            border: 'none',
            borderRadius: 999,
            padding: '7px 14px',
            cursor: applyDisabled || applying ? 'not-allowed' : 'pointer',
          }}
        >
          {applying ? t('treatment.applying') : t('treatment.apply')}
        </button>
      )}
    </div>
  );
}
