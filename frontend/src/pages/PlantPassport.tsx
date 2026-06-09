import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { Star } from 'lucide-react';
import { PassportTimeline } from '@/components/detection/DetectionList';
import { usePlants, usePassport, useTreatments } from '@/hooks/useApi';
import { api } from '@/services/api';
import type { DiseaseClass, DetectionSeverity, PlantSummary } from '@/types';
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

function severityColor(s: DetectionSeverity | null) {
  return s === 'critical' ? '#ef4444' : s === 'warning' ? '#f59e0b' : 'var(--forest-3)';
}

export function PlantPassportPage() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const { data: plants, loading: plantsLoading } = usePlants();
  // No auto-select: the user picks a plant first.
  const [selected, setSelected] = useState<string | null>(searchParams.get('plant'));

  const { data: passport, loading, error, refetch } = usePassport(selected);

  function choose(plantId: string) {
    setSelected(plantId || null);
    setSearchParams(plantId ? { plant: plantId } : {});
  }

  const subtitle = passport
    ? t('plantPassport.subtitleFor', {
        id: String(passport.plantIndex).padStart(3, '0'),
        row: passport.row,
      })
    : t('plantPassport.subtitlePick');

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('plantPassport.title')}</h1>
          <p className={styles.pageSub}>{subtitle}</p>
        </div>
        {plants && plants.length > 0 && (
          <select className={styles.select} value={selected ?? ''} onChange={(e) => choose(e.target.value)}>
            <option value="">{t('plantPassport.choose')}</option>
            {plants.map((p) => (
              <option key={p.plantId} value={p.plantId}>
                {p.plantId}{p.row != null ? ` · ${t('plants.rowShort', { row: p.row })}` : ''}
              </option>
            ))}
          </select>
        )}
      </div>

      {/* Loading / empty plant list */}
      {plantsLoading && !plants && (
        <div className={styles.card}><p className={styles.empty}>{t('common.loading')}</p></div>
      )}
      {plants && plants.length === 0 && (
        <div className={styles.card}><p className={styles.empty}>{t('plants.empty')}</p></div>
      )}

      {/* No plant chosen yet → friendly picker */}
      {plants && plants.length > 0 && !selected && (
        <div className={styles.card}>
          <div className={styles.prompt}>
            <div className={styles.promptIcon}><Star size={22} /></div>
            <div className={styles.promptTitle}>{t('plantPassport.pickTitle')}</div>
            <div className={styles.promptSub}>{t('plantPassport.pickSub', { count: plants.length })}</div>
            <div className={styles.pickGrid}>
              {plants.map((p) => (
                <PlantPickButton key={p.plantId} plant={p} onPick={() => choose(p.plantId)} />
              ))}
            </div>
          </div>
        </div>
      )}

      {/* A plant is chosen */}
      {selected && loading && !passport && (
        <div className={styles.card}><p className={styles.empty}>{t('common.loading')}</p></div>
      )}
      {selected && error && !passport && (
        <div className={styles.card}>
          <p className={styles.empty}>{t('common.loadError')}</p>
          <div style={{ marginTop: 12 }}>
            <button className={styles.btn} onClick={refetch}>{t('common.retry')}</button>
          </div>
        </div>
      )}
      {passport && <PassportTimeline passport={passport} />}
      {selected && passport && (
        <ApplyTreatmentPanel plantId={selected} onApplied={refetch} />
      )}
    </div>
  );
}

function PlantPickButton({ plant, onPick }: { plant: PlantSummary; onPick: () => void }) {
  const { t } = useTranslation();
  return (
    <button className={styles.pickItem} onClick={onPick}>
      <span className={styles.pickDot} style={{ background: severityColor(plant.latestSeverity) }} />
      <span>
        <span className={styles.pickName}>{plant.plantId}</span>
        <span className={styles.pickRow}>
          {plant.row != null ? t('plants.rowShort', { row: plant.row }) : t('plants.noScans')}
        </span>
      </span>
    </button>
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
