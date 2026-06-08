import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LoginComponent } from './core/auth/login.component';
import { FileListComponent } from './storage/pages/file-list/file-list.component';
import { FilePreviewComponent } from './storage/pages/file-preview/file-preview.component';
import { FileUploadComponent } from './storage/pages/file-upload/file-upload.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'storage/files',
    component: FileListComponent,
    canActivate: [authGuard]
  },
  {
    path: 'storage/upload',
    component: FileUploadComponent,
    canActivate: [authGuard]
  },
  {
    path: 'storage/files/:id/preview',
    component: FilePreviewComponent,
    canActivate: [authGuard]
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
