import React, { Component } from 'react';
import { LinkContainer } from 'react-router-bootstrap';
import { Button } from 'react-bootstrap';

export class Login extends Component {
    displayName = Login.name;

    constructor(props) {
        super(props);

        this.state = {
            email: "", password: "", isRequesting: false
        };

        this.login = this.login.bind(this);
        this.setEmail = this.setEmail.bind(this);
        this.setPassword = this.setPassword.bind(this);
    }

    setEmail(email) {
        this.setState({ email });
    }

    setPassword(password) {
        this.setState({ password });
    }

    login() {
        fetch("api/User/login", {
            method: "POST",
            body: JSON.stringify({ this.state.email, this.state.password }),
            headers: {
                'Content-Type': 'application/json'
                // 'Content-Type': 'application/x-www-form-urlencoded',
            }
        })
            .then(response => response.json())
            .then(data => {
                this.props.onLogin(data);
            });
    }

    render() {
        return (
            <div>
                <h1>Login</h1>
                <div><span class="label">Epost: </span><input type="text" onChange={this.setEmail}></input></div>
                <div><span class="label">Passord: </span><input type="text" onChange={this.setPassword}></input></div>
            </div>
        );
    }
}