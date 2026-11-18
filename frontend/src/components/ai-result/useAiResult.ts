import { useCallback, useEffect, useRef, useState } from 'react';
import type { AiResult } from '../../types/AiResult';
import { aiResultService } from '../../services/aiResultService';

type Mode = 'first' | 'retry' | 'poll';

export function useAiResult(requestId: string) {
    const [data, setData] = useState<AiResult | null>(null);
    const [loading, setLoading] = useState(true);
    const [retrying, setRetrying] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const seq = useRef(0); // bỏ qua phản hồi cũ nếu đã có yêu cầu mới hơn

    const load = useCallback(async (mode: Mode) => {
        const my = ++seq.current;
        if (mode === 'first') setLoading(true);
        if (mode === 'retry') setRetrying(true);
        try {
            const res = await aiResultService.getAiResult(requestId);
            if (my !== seq.current) return;
            setData(res);
            setError(null);
        } catch (e) {
            if (my !== seq.current) return;
            setData(null);
            setError(e instanceof Error ? e.message : 'Lỗi không xác định.');
        } finally {
            if (my === seq.current) { setLoading(false); setRetrying(false); }
        }
    }, [requestId]);

    useEffect(() => { void load('first'); }, [load]);

    // Máy chủ báo đang xử lý / đang thử lại thì hỏi lại mỗi 3 giây
    const status = data?.status;
    useEffect(() => {
        if (status !== 'processing' && status !== 'retrying') return;
        const t = setInterval(() => { void load('poll'); }, 3000);
        return () => clearInterval(t);
    }, [status, load]);

    const reload = useCallback(() => load('retry'), [load]);
    return { data, loading, retrying, error, reload };
}