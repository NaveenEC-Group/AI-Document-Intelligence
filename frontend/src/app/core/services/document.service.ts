import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DocumentExtractionResponse,
  DocumentListItem,
  DocumentUploadResponse,
} from '../models/document.models';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/documents`;

  upload(file: File): Observable<DocumentUploadResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<DocumentUploadResponse>(
      `${this.baseUrl}/upload`,
      formData
    );
  }

  getDocuments(search?: string, status?: string): Observable<DocumentListItem[]> {
    let params = new HttpParams();
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    if (status?.trim()) {
      params = params.set('status', status.trim());
    }

    return this.http.get<DocumentListItem[]>(this.baseUrl, { params });
  }

  getDocument(documentId: number): Observable<DocumentExtractionResponse> {
    return this.http.get<DocumentExtractionResponse>(
      `${this.baseUrl}/${documentId}`
    );
  }

  deleteDocument(documentId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${documentId}`);
  }
}
