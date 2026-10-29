import { useMemo } from 'react';
import type { SuggestedLocation } from '../../types/AiResult';
import { aiResultService } from '../../services/aiResultService';
import { authService } from '../../services/authService';
import { useAiResult } from './useAiResult';
import { layViewState } from './aiResultUtils';
import { EmptyState, ErrorState, LoadingState, ProcessingState, RetryingState } from './AiResultStates';
import { ImagePanel, LocationsPanel, MetaPanel, ObjectsPanel, OcrPanel, PartialTable } from './AiResultPanels';
import AiReviewForm from './AiReviewForm';

interface Props {
    requestId: string;
    onApplyLocation?: (l: SuggestedLocation) => void;
}

export default function AiResultView({ requestId, onApplyLocation }: Props) {
    const { data, loading, retrying, error, reload } = useAiResult(requestId);
    const view = layViewState(data, loading, retrying, error);

    const reviewItems = useMemo(() => (data ? [
        ...data.ocr.map((o, i) => ({ id: `ocr-${i}`, label: `OCR: ${o.text}` })),
        ...data.objects.map((o, i) => ({ id: `obj-${i}`, label: `Đối tượng: ${o.label}` })),
        ...data.locations.map((l, i) => ({ id: `loc-${i}`, label: `Địa điểm: ${l.name}` })),
    ] : []), [data]);

    return (
        <div className= "flex flex-col gap-3.5" >
        <div role="note" className = "rounded-md bg-navy px-4 py-3 text-sm leading-relaxed text-white" >
            <strong>Khuyến cáo: </strong> kết quả do AI tạo ra chỉ mang tính tham khảo kỹ thuật ban đầu, <strong>không thay thế</strong > kết luận
        giám định chính thức.Cán bộ thụ lý chịu trách nhiệm kiểm tra thực địa trước khi áp dụng vào hồ sơ.
      </div>

    { view === 'loading' && <LoadingState /> }
    { view === 'processing' && <ProcessingState progress={ data?.progress } engine = { data?.engine } />}
    { view === 'retrying' && <RetryingState attempt={ data?.attempt } /> }
    { view === 'error' && <ErrorState error={ data?.error } message = { error } onRetry = {() => void reload() } />
}

{ view === 'empty' && data && (<><MetaPanel data={ data } /><EmptyState onRetry={() => void reload()} / > </>) }

{
    (view === 'success' || view === 'partial') && data && (
        <>
        { view === 'partial' && <PartialTable fields={ data.missingFields } />
}
<div className="grid grid-cols-1 items-start gap-3.5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)]" >
    <div className="flex flex-col gap-3.5" >
        <MetaPanel data={ data } />
            < ImagePanel image = { data.image } ocr = { data.ocr } objects = { data.objects } />
                </div>
                < div className = "flex flex-col gap-3.5" >
                    <OcrPanel items={ data.ocr } />
                        < ObjectsPanel items = { data.objects } />
                            <LocationsPanel items={ data.locations } onApply = { onApplyLocation } />
                                </div>
                                </div>
                                < AiReviewForm
items = { reviewItems }
initial = { data.review }
onSave = {(input) => aiResultService.saveReview(data.requestId, input, authService.getSession()?.user.hoTen)}
          />
    </>
      )}
</div>
  );
}