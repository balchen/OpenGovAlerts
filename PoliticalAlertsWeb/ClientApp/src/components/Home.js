import React, { Component } from 'react';
import { LinkContainer } from 'react-router-bootstrap';
import { Button } from 'react-bootstrap';
import Modal from 'react-modal';
import { AddObserver } from './AddObserver';

export class Home extends Component {
    displayName = Home.name;

    constructor(props) {
        super(props);

        this.state = {
            model: {}, loading: true, showAddObserverWindow: false
        };

        fetch("api/Member/getIndex")
            .then(response => response.json())
            .then(data => {
                this.setState({ model: data, loading: false });
            });

        this.renderModel = this.renderModel.bind(this);
        this.showAddObserver = this.showAddObserver.bind(this);
        this.closeAddObserver = this.closeAddObserver.bind(this);
        this.addObserver = this.addObserver.bind(this);
    }

    showAddObserver() {
        this.setState({
            showAddObserverWindow: true
        });
    }

    closeAddObserver() {
        this.setState({
            showAddObserverWindow: false
        });
    }

    addObserver(observer) {
        fetch("api/Member/addObserver", {
            method: "POST",
            body: JSON.stringify({ observer }),
            headers: {
                'Content-Type': 'application/json'
                // 'Content-Type': 'application/x-www-form-urlencoded',
            }
        })
            .then(response => response.json())
            .then(data => {
                this.closeAddObserver();
                this.props.history.push('/Observer/' + data.id);
            });
    }

    renderModel(model) {
        let observers = model.observers.map((observer) =>
            <LinkContainer key={observer.id} to={'/Observer/' + observer.id}>
                <Button>{observer.name}</Button>
            </LinkContainer>
        );

        return <div>{observers} <button onClick={this.showAddObserver} className="btn btn-default">+</button></div>;
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderModel(this.state.model);

        return (
            <div>
                <h1>GovAlerts</h1>
                {contents}
                <Modal
                    isOpen={this.state.showAddObserverWindow}
                    onRequestClose={this.closeAddObserver}
                    contentLabel="Legg til observatør"
                    style={{ overlay: { zIndex: 9999 } }}
                >
                    <h3>Legg til observatør</h3>
                    <AddObserver onSave={this.addObserver} />
                </Modal>
            </div>
        );
    }
}
