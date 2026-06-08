import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
  id: number;
  type: ToastType;
  text: string;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private nextId = 1;
  private readonly messagesSubject = new BehaviorSubject<ToastMessage[]>([]);

  readonly messages$ = this.messagesSubject.asObservable();

  success(text: string): void {
    this.show('success', text);
  }

  error(text: string): void {
    this.show('error', text);
  }

  info(text: string): void {
    this.show('info', text);
  }

  dismiss(id: number): void {
    this.messagesSubject.next(
      this.messagesSubject.value.filter((message) => message.id !== id)
    );
  }

  private show(type: ToastType, text: string): void {
    const message = {
      id: this.nextId++,
      type,
      text
    };

    this.messagesSubject.next([...this.messagesSubject.value, message]);
    window.setTimeout(() => this.dismiss(message.id), 4500);
  }
}
