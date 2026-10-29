import type { AiResult, AiViewState, BBox } from '../../types/AiResult';

// Dưới ngưỡng này thì gắn cảnh báo "độ tin cậy thấp" (có thể chỉnh)
export const LOW_CONFIDENCE = 0.8;

const NO_DATA = 'Chưa có dữ liệu';

export const pct = (c?: number | null) =>
    typeof c === 'number' && !Number.isNaN(c) ? `${(c * 100).toFixed(1)}%` : '—';

export const text = (v?: string | number | null, fallback = NO_DATA) =>
    v === undefined || v === null || v === '' ? fallback : String(v);

export const dt = (iso?: string) => {
    if (!iso) return NO_DATA;
    const d = new Date(iso);
    return Number.isNaN(d.getTime()) ? NO_DATA : d.toLocaleString('vi-VN');
};

export const bboxText = (b?: BBox) => (b ? `[${b.join(', ')}]` : 'Không có tọa độ');

export function layViewState(
    data: AiResult | null,
    loading: boolean,
    retrying: boolean,
    error: string | null,
): AiViewState {
    if (retrying) return 'retrying';
    if (loading) return 'loading';
    if (error || !data || data.status === 'failed') return 'error';
    if (data.status === 'retrying') return 'retrying';
    if (data.status === 'processing') return 'processing';
    if (data.ocr.length + data.objects.length + data.locations.length === 0) return 'empty';
    return data.missingFields.some((f) => f.status !== 'done') ? 'partial' : 'success';
}