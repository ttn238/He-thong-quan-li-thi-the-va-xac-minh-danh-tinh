import { useMemo } from 'react';
import { aiResultService } from '../../services/aiResultService';
import { authService } from '../../services/authService';
import { useAiResult } from './useAiResult';
import { layViewState } from './aiResultUtils';
import { EmptyState, ErrorState, LoadingState, ProcessingState, RetryingState } from './AiResultStates';
import { ImagePanel, LocationsPanel, MetaPanel, ObjectsPanel, OcrPanel, PartialTable } from './AiResultPanels';
import AiReviewForm from './AiReviewForm';

export default function AiResultView({ requestId }: { requestId: string }) {
    const { data, loading, retrying, error, reload } = useAiResult(requestId);
    const view = layViewState(data, loading, retrying, error);
    const roleId = authService.getSession()?.user.maVaiTro;
    const canReview = roleId === 2 || roleId === 3;
    const reviewItems = useMemo(() => data ? [
        ...data.ocr.map((item, i) => ({ id: `ocr-${i}`, label: `OCR: ${item.text}` })),
        ...data.objects.map((item, i) => ({ id: `obj-${i}`, label: `Đối tượng: ${item.label}` })),
        ...data.locations.map((item, i) => ({ id: `loc-${i}`, label: `Địa điểm: ${item.name}` })),
    ] : [], [data]);

    return <div className="flex flex-col gap-3.5">
        <div role="note" className="rounded-md bg-navy px-4 py-3 text-sm leading-relaxed text-white">
            <strong>Lưu ý:</strong> kết quả AI chỉ mang tính tham khảo, không thay thế kết luận giám định. Cán bộ cần kiểm tra và xác minh trước khi cập nhật hồ sơ.
        </div>
        {view === 'loading' && <LoadingState />}
        {view === 'processing' && <ProcessingState progress={data?.progress} engine={data?.engine} />}
        {view === 'retrying' && <RetryingState attempt={data?.attempt} />}
        {view === 'error' && <ErrorState error={data?.error} message={error} onRetry={() => void reload()} />}
        {view === 'empty' && <EmptyState onRetry={() => void reload()} />}
        {data && (view === 'success' || view === 'partial' || view === 'empty') && <>
            {view === 'partial' && <PartialTable fields={data.missingFields} />}
            <div className="grid grid-cols-1 items-start gap-3.5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)]">
                <div className="flex flex-col gap-3.5">
                    <MetaPanel data={data} />
                    <ImagePanel requestId={requestId} image={data.image} ocr={data.ocr} objects={data.objects} />
                </div>
                <div className="flex flex-col gap-3.5">
                    <OcrPanel items={data.ocr} />
                    <ObjectsPanel items={data.objects} />
                    <LocationsPanel items={data.locations} />
                </div>
            </div>
            {canReview
                ? <AiReviewForm items={reviewItems} initial={data.review}
                    onSave={(input) => aiResultService.saveReview(data.requestId, input)} />
                : <p className="ai-muted">Chỉ cán bộ xác minh có quyền mới có thể lưu biên bản thẩm định.</p>}
        </>}
    </div>;
}
