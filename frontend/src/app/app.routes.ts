import { Routes } from '@angular/router';
import { DocumentWorkspaceComponent } from './features/documents/document-workspace.component';

export const routes: Routes = [
  { path: '', component: DocumentWorkspaceComponent },
  { path: '**', redirectTo: '' },
];
