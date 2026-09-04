export const DOCUMENT_VALIDATION = {
  maxFileBytes: 10 * 1024 * 1024,
  maxFileNameLength: 255,
  allowedExtensions: ['.pdf'] as const,
  allowedMimeTypes: ['application/pdf'] as const,
  pdfMagic: [0x25, 0x50, 0x44, 0x46] as const, // %PDF
};

export type FileValidationResult =
  | { ok: true }
  | { ok: false; message: string };

export function validateSelectedFile(file: File | null | undefined): FileValidationResult {
  if (!file) {
    return { ok: false, message: 'Please select a PDF file.' };
  }

  if (!file.name?.trim()) {
    return { ok: false, message: 'File name is required.' };
  }

  if (file.name.length > DOCUMENT_VALIDATION.maxFileNameLength) {
    return {
      ok: false,
      message: `File name must be ${DOCUMENT_VALIDATION.maxFileNameLength} characters or fewer.`,
    };
  }

  if (/[<>:"|?*\u0000-\u001f]/.test(file.name) || file.name.includes('..')) {
    return { ok: false, message: 'File name contains invalid characters.' };
  }

  if (file.size <= 0) {
    return { ok: false, message: 'The selected file is empty.' };
  }

  if (file.size > DOCUMENT_VALIDATION.maxFileBytes) {
    return { ok: false, message: 'The maximum allowed file size is 10 MB.' };
  }

  const extension = getExtension(file.name);
  if (!DOCUMENT_VALIDATION.allowedExtensions.includes(extension as '.pdf')) {
    return { ok: false, message: 'Only PDF files are supported.' };
  }

  if (
    file.type &&
    !DOCUMENT_VALIDATION.allowedMimeTypes.includes(
      file.type.toLowerCase() as 'application/pdf'
    )
  ) {
    return { ok: false, message: 'Invalid file type. Upload a PDF document.' };
  }

  return { ok: true };
}

export async function validatePdfMagicBytes(file: File): Promise<FileValidationResult> {
  const header = new Uint8Array(await file.slice(0, 4).arrayBuffer());
  const expected = DOCUMENT_VALIDATION.pdfMagic;

  const matches = expected.every((byte, index) => header[index] === byte);
  if (!matches) {
    return {
      ok: false,
      message: 'File content is not a valid PDF.',
    };
  }

  return { ok: true };
}

function getExtension(fileName: string): string {
  const index = fileName.lastIndexOf('.');
  if (index < 0) {
    return '';
  }

  return fileName.slice(index).toLowerCase();
}
