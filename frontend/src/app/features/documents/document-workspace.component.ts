import {
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize, switchMap } from 'rxjs';
import { DocumentService } from '../../core/services/document.service';
import { ToastService } from '../../core/services/toast.service';
import {
  DocumentExtractionResponse,
  DocumentListItem,
  ExtractedInvoiceData,
  ExtractedLineItem,
} from '../../core/models/document.models';
import {
  validatePdfMagicBytes,
  validateSelectedFile,
} from '../../core/validation/document-validation';

@Component({
  selector: 'app-document-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './document-workspace.component.html',
  styleUrl: './document-workspace.component.css',
})
export class DocumentWorkspaceComponent implements OnInit {
  private readonly documentService = inject(DocumentService);
  private readonly toast = inject(ToastService);

  @ViewChild('fileInput') fileInput?: ElementRef<HTMLInputElement>;

  readonly isDragging = signal(false);
  readonly isProcessing = signal(false);
  readonly isLoadingList = signal(false);
  readonly isLoadingDetail = signal(false);
  readonly selectedFile = signal<File | null>(null);
  readonly result = signal<DocumentExtractionResponse | null>(null);
  readonly parsedData = signal<ExtractedInvoiceData | null>(null);
  readonly documents = signal<DocumentListItem[]>([]);
  readonly searchTerm = signal('');
  readonly statusFilter = signal('');
  readonly activeDocumentId = signal<number | null>(null);

  readonly stats = computed(() => {
    const items = this.documents();
    return {
      total: items.length,
      completed: items.filter((item) => item.status === 'Completed').length,
      failed: items.filter((item) => item.status === 'Failed').length,
      processing: items.filter((item) => item.status === 'Processing').length,
    };
  });

  readonly lineItems = computed<ExtractedLineItem[]>(() => {
    const items = this.parsedData()?.lineItems;
    return Array.isArray(items) ? items : [];
  });

  ngOnInit(): void {
    this.refreshDocuments();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);

    const files = event.dataTransfer?.files;
    if (!files?.length) {
      this.toast.error('No file was dropped.');
      return;
    }

    if (files.length > 1) {
      this.toast.error('Please upload only one PDF at a time.');
      return;
    }

    void this.setFile(files[0]);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      this.toast.error('Please select a PDF file.');
      return;
    }

    void this.setFile(file);
  }

  openFilePicker(): void {
    if (this.isProcessing()) {
      return;
    }

    this.fileInput?.nativeElement.click();
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value);
    this.refreshDocuments();
  }

  onStatusFilterChange(value: string): void {
    this.statusFilter.set(value);
    this.refreshDocuments();
  }

  clearSelection(): void {
    this.selectedFile.set(null);
    this.result.set(null);
    this.parsedData.set(null);
    this.activeDocumentId.set(null);
    this.resetFileInput();
    this.toast.success('Workspace cleared.');
  }

  processDocument(): void {
    const file = this.selectedFile();
    const validation = validateSelectedFile(file);

    if (!validation.ok) {
      this.toast.error(validation.message);
      return;
    }

    if (this.isProcessing() || !file) {
      return;
    }

    this.isProcessing.set(true);
    this.result.set(null);
    this.parsedData.set(null);
    this.toast.success('Upload started. Extracting document data…');

    this.documentService
      .upload(file)
      .pipe(
        switchMap((upload) => {
          if (!upload?.documentId || upload.documentId <= 0) {
            throw new Error('Invalid document id returned by the API.');
          }

          this.toast.success(upload.message || 'Document uploaded successfully.');
          this.activeDocumentId.set(upload.documentId);
          return this.documentService.getDocument(upload.documentId);
        }),
        finalize(() => this.isProcessing.set(false))
      )
      .subscribe({
        next: (document) => {
          this.applyDocumentResult(document, true);
          this.refreshDocuments();
        },
        error: (error: unknown) => {
          this.toast.error(this.resolveError(error));
          this.refreshDocuments();
        },
      });
  }

  openDocument(item: DocumentListItem): void {
    if (this.isLoadingDetail() || this.isProcessing()) {
      return;
    }

    this.isLoadingDetail.set(true);
    this.activeDocumentId.set(item.documentId);

    this.documentService
      .getDocument(item.documentId)
      .pipe(finalize(() => this.isLoadingDetail.set(false)))
      .subscribe({
        next: (document) => this.applyDocumentResult(document, false),
        error: (error: unknown) => this.toast.error(this.resolveError(error)),
      });
  }

  deleteDocument(item: DocumentListItem, event: Event): void {
    event.stopPropagation();

    const confirmed = window.confirm(
      `Delete "${item.fileName}"? This cannot be undone.`
    );
    if (!confirmed) {
      return;
    }

    this.documentService.deleteDocument(item.documentId).subscribe({
      next: (response) => {
        this.toast.success(response.message || 'Document deleted successfully.');
        if (this.activeDocumentId() === item.documentId) {
          this.result.set(null);
          this.parsedData.set(null);
          this.activeDocumentId.set(null);
        }
        this.refreshDocuments();
      },
      error: (error: unknown) => this.toast.error(this.resolveError(error)),
    });
  }

  async copyJson(): Promise<void> {
    const data = this.result()?.extractedData;
    if (!data) {
      this.toast.error('No extracted JSON to copy.');
      return;
    }

    try {
      await navigator.clipboard.writeText(this.prettyJson(data));
      this.toast.success('JSON copied to clipboard.');
    } catch {
      this.toast.error('Unable to copy JSON to clipboard.');
    }
  }

  downloadJson(): void {
    const document = this.result();
    if (!document?.extractedData) {
      this.toast.error('No extracted JSON to download.');
      return;
    }

    const blob = new Blob([this.prettyJson(document.extractedData)], {
      type: 'application/json',
    });
    const url = URL.createObjectURL(blob);
    const anchor = window.document.createElement('a');
    const baseName = document.fileName.replace(/\.pdf$/i, '') || 'document';
    anchor.href = url;
    anchor.download = `${baseName}-extraction.json`;
    anchor.click();
    URL.revokeObjectURL(url);
    this.toast.success('JSON download started.');
  }

  refreshDocuments(): void {
    this.isLoadingList.set(true);
    this.documentService
      .getDocuments(this.searchTerm(), this.statusFilter())
      .pipe(finalize(() => this.isLoadingList.set(false)))
      .subscribe({
        next: (items) => this.documents.set(items),
        error: (error: unknown) => this.toast.error(this.resolveError(error)),
      });
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) {
      return `${bytes} B`;
    }
    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  statusClass(status: string): string {
    const normalized = status.toLowerCase();
    if (normalized === 'completed') {
      return 'status status--ok';
    }
    if (normalized === 'failed') {
      return 'status status--bad';
    }
    if (normalized === 'processing') {
      return 'status status--busy';
    }
    return 'status';
  }

  lineItemValue(item: ExtractedLineItem, key: string): string {
    const value = item[key];
    if (value == null || value === '') {
      return '—';
    }
    return String(value);
  }

  private applyDocumentResult(
    document: DocumentExtractionResponse,
    showSuccessToast: boolean
  ): void {
    if (!document) {
      this.toast.error('Document details were not returned.');
      return;
    }

    this.result.set(document);
    this.parsedData.set(this.parseExtractedData(document.extractedData));
    this.activeDocumentId.set(document.documentId);

    if (document.status === 'Failed') {
      this.toast.error('Document processing failed.');
      return;
    }

    if (showSuccessToast) {
      this.toast.success('Extraction completed successfully.');
    } else {
      this.toast.success(`Loaded "${document.fileName}".`);
    }
  }

  private async setFile(file: File): Promise<void> {
    this.result.set(null);
    this.parsedData.set(null);
    this.activeDocumentId.set(null);

    const basic = validateSelectedFile(file);
    if (!basic.ok) {
      this.selectedFile.set(null);
      this.resetFileInput();
      this.toast.error(basic.message);
      return;
    }

    const magic = await validatePdfMagicBytes(file);
    if (!magic.ok) {
      this.selectedFile.set(null);
      this.resetFileInput();
      this.toast.error(magic.message);
      return;
    }

    this.selectedFile.set(file);
    this.toast.success(`"${file.name}" is ready to extract.`);
  }

  private resetFileInput(): void {
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }

  private parseExtractedData(raw: string): ExtractedInvoiceData | null {
    if (!raw?.trim()) {
      return null;
    }

    try {
      return JSON.parse(raw) as ExtractedInvoiceData;
    } catch {
      this.toast.error('Extracted data is not valid JSON.');
      return null;
    }
  }

  private prettyJson(raw: string): string {
    try {
      return JSON.stringify(JSON.parse(raw), null, 2);
    } catch {
      return raw;
    }
  }

  private resolveError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { message?: string; title?: string } | string | null;

      if (typeof body === 'string' && body.trim()) {
        return body;
      }

      if (body && typeof body === 'object') {
        if (body.message) {
          return body.message;
        }
        if (body.title) {
          return body.title;
        }
      }

      if (error.status === 0) {
        return 'Cannot reach the API. Start the .NET backend on port 5017.';
      }

      if (error.status === 400) {
        return 'Validation failed. Check the file and try again.';
      }

      if (error.status === 404) {
        return 'Document not found.';
      }

      if (error.status === 413) {
        return 'The uploaded file is too large.';
      }

      if (error.status >= 500) {
        return 'Server error while processing the document.';
      }

      return error.message || 'Request failed.';
    }

    if (error instanceof Error && error.message) {
      return error.message;
    }

    return 'Something went wrong while processing the document.';
  }
}
