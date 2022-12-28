import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { BaseComponent } from 'src/app/core/base.component';
import { ChatService } from 'src/app/core/services/chat/chat.service';
import { Room } from 'src/app/core/services/models/room.model';

@Component({
  selector: 'app-choose-room',
  templateUrl: './choose-room.component.html',
  styleUrls: ['./choose-room.component.scss'],
})
export class ChooseRoomComponent extends BaseComponent implements OnInit {
  form = new FormGroup({
    username: new FormControl(null, [Validators.required]),
    password: new FormControl(null, [Validators.required]),
    chatRoom: new FormControl(null, [Validators.required]),
  });

  createNewForm = new FormGroup({
    chatRoomName: new FormControl(null),
  });

  isNewRoomFormVisible = false;

  availableRooms: Room[] = [];
  constructor(private _router: Router, private _chatService: ChatService) {
    super();
  }

  ngOnInit(): void {
    this.subscriptions$.add(
      this._chatService.getRooms().subscribe((rooms) => {
        this.availableRooms = rooms;
      })
    );
  }

  goToChatRoom() {
    if (!this.form.valid) {
      console.log('invalid');
      return;
    }

    this._router.navigate(['/', this.form.get('chatRoom')?.value]);
  }

  showNewRoomForm() {}
}
