import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { off } from 'process';
import { BehaviorSubject, Observable, of, take } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Group } from '../_models/group';
import { Message } from '../_models/message';
import { User } from '../_models/User';
import { getPaginatedResult, getPaginationHeaders } from './paginationHelper';

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  baseUrl = environment.apiUrl;
  private messageThreadSource = new BehaviorSubject<Message[]>([]);
  messageThread$ = this.messageThreadSource.asObservable();
  messageHubUrl = environment.hubUrl;
  private hubConnection?: HubConnection;

  constructor(private http: HttpClient) { }

  createHubConnection(user:User, otherUserName: string){
    this.hubConnection = new HubConnectionBuilder()
    .withUrl(this.messageHubUrl + 'messages?user=' + otherUserName,{
      accessTokenFactory: () => user.token
    })
    .withAutomaticReconnect()
    .build();

    this.hubConnection.start().catch(error => console.error(error));
    
    this.hubConnection.on('RecieveMessageThread', messages => {
      this.messageThreadSource.next(messages);
    });

    this.hubConnection.on('UpdatedGroup', (group: Group)=> {
      if(group.connections.some(x => x.username === otherUserName)){
        this.messageThread$.pipe(take(1)).subscribe({
          next: messages => {
            messages.forEach(message => {
              if(!message.dateRead){
                message.dateRead = new Date(Date.now())
              }
            })
            this.messageThreadSource.next([...messages]);
          }
        })
      }
    })

    this.hubConnection.on('NewMessage', message => {
      //// spread exisitng message thread and add on new message that we recieve
      this.messageThread$.pipe(take(1)).subscribe({
        next: messages => {
          this.messageThreadSource.next([...messages, message]);
        }
      })
    })
  }

  stopHubConnection(){
    if(this.hubConnection){
    this.hubConnection.stop().catch(error => console.log(error));
    }
  }

  getMessages(pageNumber: number, pageSize: number, container: string){
    let params = getPaginationHeaders(pageNumber, pageSize);
    params = params.append('Container', container);
    return getPaginatedResult<Message[]>(this.baseUrl + 'messages', params, this.http);
  }

  getMessageThread(userName: string){
    return this.http.get<Message[]>(this.baseUrl + 'messages/thread/' + userName);
  }

  async sendMessage(userName: string, messageContent: string){
    //// invoking the Send message created in the Messagehub.cs class
    return this.hubConnection?.invoke('SendMessage', {recipientUserName: userName, content: messageContent})
      .catch(error => console.log(error));
  }

  deleteMessage(id: number) {
    return this.http.delete(this.baseUrl + 'messages/' + id);
  }
}
