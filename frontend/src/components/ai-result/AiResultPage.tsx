import { useNavigate, useParams } from 'react-router-dom';
import AiResultView from '../../components/ai-result/AiResultView';
import AiUploadBox from '../../components/ai-result/AiUploadBox';
import { S } from '../../components/ai-result/aiResultStyles';

export default function AiResultPage() {
    const { requestId } = useParams();
    const nav = useNavigate();

    return (
        <div className= "mx-auto max-w-300 px-4 py-4 md:px-6" >
        <div className="mb-3 flex items-center justify-between gap-2" >
            <h2 className="m-0 text-xl font-bold text-navy" >
                Phân tích AI & Đối chiếu{ requestId ? ` · #${requestId}` : '' }
    </h2>
    {
        requestId && (
            <button className={ S.btnGhost } onClick = {() => nav('/identification/ai-result')
    }> Phân tích ảnh khác </button>
        )
}
</div>
{
    requestId
        ? <AiResultView key={ requestId } requestId = { requestId } />
        : <AiUploadBox onUploaded={ (id) => nav(`/identification/ai-result/${id}`) } />
}
</div>
  );
}