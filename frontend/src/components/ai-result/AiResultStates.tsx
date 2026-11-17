import type { AiResultError } from '../../types/AiResult';
import { S } from './aiResultStyles';
import { dt, text } from './aiResultUtils';

export function LoadingState() {
    return <div className={S.state} role="status">Đang tải kết quả phân tích…</div>;
}

export function ProcessingState({ progress, engine }: { progress?: number; engine?: string }) {
    const value = Math.min(100, Math.max(0, progress ?? 0));
    return <div className={S.state} role="status">
        <h3 className="m-0 mb-2 text-base font-semibold">Đang phân tích hình ảnh</h3>
        <p className={S.muted}>Động cơ: {text(engine)}. Trang sẽ tự cập nhật khi có kết quả.</p>
        <div className="mx-auto my-3 h-2 max-w-105 overflow-hidden rounded bg-line">
            <div className="h-full bg-brand transition-all" style={{ width: `${value}%` }} />
        </div>
        <p className={S.muted}>{progress === undefined ? 'Đang xử lý' : `${value}% hoàn tất`}</p>
    </div>;
}

export function RetryingState({ attempt }: { attempt?: number }) {
    return <div className={S.state} role="status">
        <h3 className="m-0 mb-2 text-base font-semibold">Đang thử lấy kết quả</h3>
        <p className={S.muted}>Vui lòng chờ{attempt ? ` (lần ${attempt})` : ''}.</p>
    </div>;
}

export function EmptyState({ onRetry }: { onRetry: () => void }) {
    return <div className={S.state}>
        <h3 className="m-0 mb-2 text-base font-semibold">Ảnh chưa có kết quả nhận diện</h3>
        <p className={S.muted}>Thử ảnh rõ nét, đủ sáng hoặc tiếp tục kiểm tra thủ công.</p>
        <button className={S.btn} onClick={onRetry}>Tải lại kết quả</button>
    </div>;
}

export function ErrorState({ error, message, onRetry }: { error?: AiResultError; message?: string | null; onRetry: () => void }) {
    return <div className="rounded-md border border-red-400 bg-red-50 p-6 text-center" role="alert">
        <h3 className="m-0 mb-2 text-base font-semibold text-red-800">
            {error?.httpStatus ? `Lỗi dịch vụ (HTTP ${error.httpStatus})` : 'Không tải được kết quả AI'}
        </h3>
        <p className="m-0 mb-3 text-sm">{text(error?.message ?? message, 'Không thể lấy kết quả phân tích.')}</p>
        {error && <dl className={`${S.kv} mx-auto mb-4 max-w-md text-left font-mono text-xs`}>
            <dt>Mã lỗi</dt><dd>{text(error.code)}</dd>
            <dt>Thời điểm</dt><dd>{dt(error.occurredAt)}</dd>
        </dl>}
        <button className={S.btn} onClick={onRetry}>Thử lại</button>
    </div>;
}
