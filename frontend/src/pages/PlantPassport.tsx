import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { PassportTimeline } from '@/components/detection/DetectionList';
import { usePlants, usePassport, useTreatments } from '@/hooks/useApi';
import { api } from '@/services/api';
import type { DiseaseClass } from '@/types';
import styles from './shared.module.css';

const DISEASES: DiseaseClass[] = [
  'late_blight',
  'early_blight',
  'fusarium_wilt',
  'powdery_mildew',
  'bacterial_spot',
  'leaf_mold',
  'septoria_leaf_spot',
  'spider_mites',
];

export function PlantPassportPage() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const { data: plants } = usePlants();
  const [selected, setSelected] = useState<string | null>(searchParams.get('plant'));

  // Default to the first plant once the list loads and nothing is chosen.
  useEffect(() => {
    if (!selected && plants && plants.length > 0) {
      setSelected(plants[0].plantId);
    }
  }, [plants, selected]);

  const { data: passport, loading, error, refetch } = usePassport(selected);

  function choose(plantId: string) {
    setSelected(plantId);
    setSearchParams(plantId ? { plant: plantId } : {});
  }

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('plantPassport.title')}</h1>
          <p className={styles.pageSub}>{t('plantPassport.subtitle')}</p>
        </div>
        {plants && plants.length > 0 && (
          <select
            className={styles.select}
            value={selected ?? ''}
            onChange={(e) => choose(e.target.value)}
          >
            {plants.map((p) => (
              <option key={p.plantId} value={p.plantId}>
                {p.plantId}{p.row != null ? ` · ${t('plants.rowShort', { row: p.row })}` : ''}
              </option>
            ))}
          </select>
        )}
      </div>

      {plants && plants.length === 0 && (
        <div className={styles.card}><p className={styles.empty}>{t('plants.empty')}</p></div>
      )}
      {selected && loading && (
        <div className={styles.card}><p className={styles.empty}>{t('common.loading')}</p></div>
      )}
      {selected && error && !passport && (
        <div className={styles.card}><p className={styles.empty}>{t('common.loadError')}</p></div>
      )}
      {passport && <PassportTimeline passport={passport} />}

      {selected && passport && (
        <ApplyTreatmentPanel plantId={selected} onApplied={refetch} />
      )}
    </div>
  );
}

function ApplyTreatmentPanel({ plantId, onApplied }: { plantId: string; onApplied: () => void }) {
  const { t } = useTranslation();
  const [disease, setDisease] = useState<DiseaseClass>('late_blight');
  const { data: treatments } = useTreatments(disease);
  const [treatmentId, setTreatmentId] = useState<string>('');
  const [applying, setApplying] = useState(false);
  const [feedback, setFeedback] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null);

  // Default the treatment selector to the first option whenever the list changes.
  useEffect(() => {
    if (treatments && treatments.length > 0) {
      setTreatmentId((cur) => (treatments.some((tr) => tr.id === cur) ? cur : treatments[0].id));
    } else {
      setTreatmentId('');
    }
  }, [treatments]);

  async function apply() {
    if (!treatmentId) return;
    setApplying(true);
    setFeedback(null);
    try {
      await api.recordAppliedTreatment({ treatmentId, plantId, appliedAt: new Date().toISOString() });
      const name = treatments?.find((tr) => tr.id === treatmentId)?.name ?? '';
      setFeedback({ kind: 'ok', text: t('treatment.applied', { name, plant: plantId }) });
      onApplied();
    } catch {
      setFeedback({ kind: 'err', text: t('treatment.applyError') });
    } finally {
      setApplying(false);
    }
  }

  return (
    <div className={styles.card} style={{ marginTop: 14 }}>
      <div className={styles.cardTitle}>{t('passportApply.title')}</div>
      <div className={styles.cardSub}>{t('passportApply.hint', { plant: plantId })}</div>

      <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', alignItems: 'center' }}>
        <select className={styles.select} value={disease} onChange={(e) => setDisease(e.target.value as DiseaseClass)}>
          {DISEASES.map((d) => (
            <option key={d} value={d}>{t(`disease.${d}`)}</option>
          ))}
        </select>
        <select
          className={styles.select}
          value={treatmentId}
          onChange={(e) => setTreatmentId(e.target.value)}
          disabled={!treatments || treatments.length === 0}
        >
          {(treatments ?? []).map((tr) => (
            <option key={tr.id} value={tr.id}>{tr.name} — {tr.dosage}</option>
          ))}
        </select>
        <button className={styles.btn} onClick={apply} disabled={applying || !treatmentId}>
          {applying ? t('treatment.applying') : t('treatment.apply')}
        </button>
      </div>

      {feedback && (
        <p className={styles.empty} style={{ marginTop: 10, color: feedback.kind === 'ok' ? 'var(--forest)' : 'var(--red-txt, #b91c1c)' }}>
          {feedback.text}
        </p>
      )}
    </div>
  );
}
