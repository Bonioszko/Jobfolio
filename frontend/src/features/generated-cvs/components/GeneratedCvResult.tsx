import { ErrorMessage } from "../../../components/common/ErrorMessage";
import { getPdfDownloadUrl } from "../../cv-compilation/api/cvCompilationApi";
import { useCvCompilation } from "../../cv-compilation/hooks/useCvCompilation";
import type { GeneratedCv } from "../types/generatedCv";

type GeneratedCvResultProps = {
  document: GeneratedCv;
};

export function GeneratedCvResult({ document }: GeneratedCvResultProps) {
  const compilation = useCvCompilation();
  const pdfUrl = compilation.pdfArtifactId
    ? getPdfDownloadUrl(compilation.pdfArtifactId)
    : undefined;

  return (
    <section aria-label="Generated CV">
      <textarea aria-label="Generated TeX" value={document.tex} readOnly />
      <button
        disabled={compilation.isCompiling}
        onClick={() => void compilation.compile(document.versionId)}
      >
        {compilation.isCompiling ? "Compiling…" : "Compile PDF"}
      </button>
      {compilation.progress && <p className="progress">{compilation.progress}</p>}
      <ErrorMessage message={compilation.error} />
      {pdfUrl && (
        <>
          <iframe className="pdf-preview" title="PDF preview" src={pdfUrl} />
          <a className="download" href={pdfUrl}>
            Download PDF
          </a>
        </>
      )}
    </section>
  );
}
