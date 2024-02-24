import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { Observer } from './components/Observer';
import { Search } from './components/Search';

export default class App extends Component {
    displayName = App.name;

    constructor(props) {
        super(props);

        this.state = {
            user: null
        };
    }

    loginCompleted(user) {
        this.setState({ user });
    }

    render() {
        if (this.state.user == null)
            this.props.history.push('/Login');

        return (
            <Layout>
                <Route exact path='/' component={Home} />
                <Route exact path='/Login' render={() => <Login onLogin={loginCompleted} />} />
                <Route exact path='/Observer/:id' render={({ match }) => <Observer id={match.params.id} />} />
                <Route exact path='/Observer/:oid/Search/:sid' render={({ match }) => <Search id={match.params.sid} />} />
            </Layout>
        );
    }
}