import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { off } from 'process';
import { Observable, of } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Message } from '../_models/message';
import { getPaginatedResult, getPaginationHeaders } from './paginationHelper';

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  baseUrl = environment.apiUrl;
  messageThread$: Observable<Message[] | null> = of(null);

  constructor(private http: HttpClient) { }

  getMessages(pageNumber: number, pageSize: number, container: string){
    let params = getPaginationHeaders(pageNumber, pageSize);
    params = params.append('Container', container);
    return getPaginatedResult<Message[]>(this.baseUrl + 'messages', params, this.http);
  }

  getMessageThread(userName: string){
    return this.http.get<Message[]>(this.baseUrl + 'messages/thread/' + userName);
  }

  sendMessage(userName: string, messageContent: string){
    return this.http.post<Message>(this.baseUrl + 'messages', 
    {recipientUserName: userName, content: messageContent});
  }

  deleteMessage(id: number) {
    return this.http.delete(this.baseUrl + 'messages/' + id);
  }
}
