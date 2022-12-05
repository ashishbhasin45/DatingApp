import { Component, OnInit } from '@angular/core';
import { Message } from '../_models/message';
import { Pagination } from '../_models/pagination';
import { MessageService } from '../_services/message.service';

@Component({
  selector: 'app-messages',
  templateUrl: './messages.component.html',
  styleUrls: ['./messages.component.css']
})
export class MessagesComponent implements OnInit {
  messages?: Message[];
  pagination?: Pagination;
  container = 'Inbox';
  pageNumber = 1;
  pageSize = 5;
  loading = false;

  constructor(private messageService: MessageService) { }

  ngOnInit(): void {
    this.loadMessages();
  }

  loadMessages(){
    this.loading = true;
    this.messageService.getMessages(this.pageNumber, this.pageSize, this.container).subscribe({
      next: response => {
        this.messages = response.result;
        this.pagination = response.pagination;
        this.loading = false;
      }
    })
  }
 
  pageChange(event: any){
    if (this.pageNumber !== event.page){
      this.pageNumber = event.page;
      this.loadMessages();
    }
  }

  deleteMessage(messageId: number){
    this.messageService.deleteMessage(messageId).subscribe({
      next: () => this.messages?.splice(this.messages.findIndex(m => m.id === messageId), 1)
    })
  }
}
