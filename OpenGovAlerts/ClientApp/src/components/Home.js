import React, { Component } from 'react';
import { LinkContainer } from 'react-router-bootstrap';

export class Home extends Component {
    static renderModel(model) {
        let observers = model.observers.each((observer) =>
            <LinkContainer to={'/Observer/' + observer.Id} exact>
                {observer.name}
            </LinkContainer>
        );

        return observers;
    }

    displayName = Home.name

    constructor(props) {
        super(props);

        this.state = {
            model: {}, loading: true
        };

        fetch("api/Member/getIndex")
            .then(response => response.json())
            .then(data => {
                this.setState({ model: data, loading: false });
            });
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : Home.renderModel(this.state.model);

        return (
            <div>
                <h1>GovAlerts</h1>
                {contents}
            </div>
        );
    }
}
