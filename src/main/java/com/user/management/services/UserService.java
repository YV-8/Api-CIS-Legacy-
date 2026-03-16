package com.user.management.services;

import java.util.List;

import org.modelmapper.ModelMapper;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;

import com.user.management.dtos.UpdateUserRequestDTO;
import com.user.management.exceptions.ResourceNotFoundException;
import com.user.management.models.User;
import com.user.management.dtos.UserResponseDTO;
import com.user.management.repository.UserRepository;

@Service
public class UserService {

    @Autowired
    private  ModelMapper modelMapper;

    @Autowired
    private UserRepository userRepository;
    

    public List<UserResponseDTO> getAllUsers() {
        return userRepository.findAll()
                .stream()
                .map(user -> modelMapper.map(user, UserResponseDTO.class))
                .toList();
    }


    public UserResponseDTO updateUser(String id, UpdateUserRequestDTO request) {
        User user = userRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("User not found with id: " + id));

        if (request.getLogin() != null
                && !request.getLogin().isBlank()
                && userRepository.existsByLoginAndIdNot(request.getLogin(), id)) {
            throw new DataIntegrityViolationException("Login already exists");
        }

        if (request.getName() != null && !request.getName().isBlank()) {
            user.setName(request.getName());
        }

        if (request.getLogin() != null && !request.getLogin().isBlank()) {
            user.setLogin(request.getLogin());
        }

        if (request.getPassword() != null && !request.getPassword().isBlank()) {
            user.setPassword(request.getPassword());
        }

        User updatedUser = userRepository.save(user);
        return modelMapper.map(updatedUser, UserResponseDTO.class);
    }
    
}
