import { Injectable, signal } from '@angular/core';
import { ToastKind, ToastMessage } from '../models/toast.models';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();

  readonly toasts = signal<ToastMessage[]>([]);

  success(text: string, durationMs = 3500): void {
    this.show('success', text, durationMs);
  }

  error(text: string, durationMs = 5000): void {
    this.show('error', text, durationMs);
  }

  dismiss(id: number): void {
    const timer = this.timers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.timers.delete(id);
    }

    this.toasts.update((items) => items.filter((toast) => toast.id !== id));
  }

  private show(kind: ToastKind, text: string, durationMs: number): void {
    const message: ToastMessage = {
      id: this.nextId++,
      kind,
      text,
    };

    this.toasts.update((items) => [...items, message]);

    const timer = setTimeout(() => this.dismiss(message.id), durationMs);
    this.timers.set(message.id, timer);
  }
}
