import type { AiResultError } from '../../types/AiResult';
import { S } from './aiResultStyles';
import { dt, text } from './aiResultUtils';

export function LoadingState() {
    return <div className={ S.state } role = "status" > Đang tải kết quả phân tích...</div>;
}

export function ProcessingState({ progress, engine }: { progress?: number; engine?: string }) {
    const p = Math.min(100, Math.max(0, progress ?? 0));
    return (
        <div className= { S.state } role = "status" >
            <h3 className="m-0 mb-2 text-base font-semibold" > Đang phân tích hình ảnh chứng cứ </h3>
                < p className = { S.muted } > Động cơ: { text(engine) }. Trang sẽ tự cập nhật khi có kết quả.</p>
                    < div className = "mx-auto my-3 h-2 max-w-105 overflow-hidden rounded bg-line" >
                        <div className="h-full bg-brand transition-all duration-300" style = {{ width: `${p}%` }
} />
    </div>
    < p className = { S.muted } > { progress === undefined ? 'Chưa có tiến độ' : `${p}% hoàn tất`}</p>
        </div>
  );
}

export function RetryingState({ attempt }: { attempt?: number }) {
    return (
        <div className= { S.state } role = "status" >
            <h3 className="m-0 mb-2 text-base font-semibold" > Đang thử lại </h3>
                < p className = { S.muted } > Đang lấy lại kết quả phân tích{ attempt ? ` (lần ${attempt})` : '' }. Vui lòng chờ.</p>
                    < div className = "mx-auto my-3 h-2 max-w-105 overflow-hidden rounded bg-line" >
                        <div className="h-full w-1/3 animate-pulse bg-brand" />
                            </div>
                            </div>
  );
}

export function EmptyState({ onRetry }: { onRetry: () => void }) {
    return (
        <div className= { S.state } >
        <h3 className="m-0 mb-2 text-base font-semibold" > Không tìm thấy đặc điểm nhận dạng phù hợp </h3>
            < p className = { S.muted } > AI đã quét ảnh nhưng không nhận ra văn bản, đối tượng hay địa điểm nào đủ độ tin cậy.</p>
                < p className = "mb-1 mt-3 text-sm font-semibold" > Gợi ý cho cán bộ: </p>
                    < ul className = "m-0 mx-auto mb-4 inline-block pl-5 text-left text-sm" >
                        <li>Chụp bổ sung ảnh trực diện, đủ sáng, không ngược sáng.</li>
                            < li > Chụp cận biển số hoặc biển tên đường nếu có.</li>
                                < li > Nhập đặc điểm thủ công vào hồ sơ.</li>
                                    </ul>
                                    < div > <button className={ S.btn } onClick = { onRetry } > Tải lại kết quả < /button></div >
                                        </div>
  );
}

export function ErrorState({ error, message, onRetry }: { error?: AiResultError; message?: string | null; onRetry: () => void }) {
    return (
        <div className= "rounded-md border border-red-400 bg-red-50 p-6 text-center" role = "alert" >
            <h3 className="m-0 mb-2 text-base font-semibold text-red-800" >
                Lỗi dịch vụ AI{ error?.httpStatus ? ` (HTTP ${error.httpStatus})` : '' }
    </h3>
        < p className = "m-0 mb-3 text-sm" > { text(error?.message ?? message, 'Không thể lấy kết quả phân tích.')
} </p>
{
    error && (
        <dl className={ `${S.kv} mx-auto mb-4 max-w-md text-left font-mono text-xs` }>
            <dt>Mã lỗi < /dt><dd>{text(error.code)}</dd >
                <dt>Trace ID < /dt><dd>{text(error.traceId)}</dd >
                    <dt>Thời điểm < /dt><dd>{dt(error.occurredAt)}</dd >
                        </dl>
      )
}
<button className={ S.btn } onClick = { onRetry } > Thử lại </button>
    </div>
  );
}